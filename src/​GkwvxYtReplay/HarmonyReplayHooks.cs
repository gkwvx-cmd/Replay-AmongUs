using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace GkwvxYtReplay;

public sealed class HarmonyReplayHooks : IDisposable
{
    private readonly Harmony _harmony;
    private readonly ReplayMatchSession _session;
    private readonly Action<string>? _logInfo;
    private readonly Action<string>? _logWarn;
    private bool _disposed;

    public HarmonyReplayHooks(
        string harmonyId,
        ReplayMatchSession session,
        Action<string>? logInfo = null,
        Action<string>? logWarn = null)
    {
        if (string.IsNullOrWhiteSpace(harmonyId))
        {
            throw new ArgumentException("O ID do Harmony não pode ser vazio.", nameof(harmonyId));
        }

        _session = session ?? throw new ArgumentNullException(nameof(session));
        _harmony = new Harmony(harmonyId);
        _logInfo = logInfo;
        _logWarn = logWarn;
    }

    public int InstallHooks()
    {
        int installed = 0;

        installed += TryPatchMethod(
            typeNames: new[] { "ShipStatus", "InnerNet.InnerNetClient" },
            methodNames: new[] { "Begin", "Start" },
            postfixMethod: AccessTools.Method(typeof(HarmonyReplayHooks), nameof(OnMatchStartedPostfix))
        );

        installed += TryPatchMethod(
            typeNames: new[] { "MeetingHud" },
            methodNames: new[] { "Start" },
            postfixMethod: AccessTools.Method(typeof(HarmonyReplayHooks), nameof(OnMeetingStartedPostfix))
        );

        installed += TryPatchMethod(
            typeNames: new[] { "PlayerControl" },
            methodNames: new[] { "MurderPlayer" },
            postfixMethod: AccessTools.Method(typeof(HarmonyReplayHooks), nameof(OnMurderPlayerPostfix))
        );

        _logInfo?.Invoke($"[HarmonyReplayHooks] Hooks instalados com sucesso: {installed}");
        return installed;
    }

    private int TryPatchMethod(string[] typeNames, string[] methodNames, MethodInfo? postfixMethod)
    {
        if (postfixMethod is null)
        {
            _logWarn?.Invoke("[HarmonyReplayHooks] Postfix nulo; patch ignorado.");
            return 0;
        }

        Type? targetType = FindTypeByCandidates(typeNames);
        if (targetType is null)
        {
            _logWarn?.Invoke($"[HarmonyReplayHooks] Nenhum tipo encontrado para: {string.Join(", ", typeNames)}");
            return 0;
        }

        MethodInfo? targetMethod = FindMethodByCandidates(targetType, methodNames);
        if (targetMethod is null)
        {
            _logWarn?.Invoke($"[HarmonyReplayHooks] Nenhum método encontrado em {targetType.FullName}: {string.Join(", ", methodNames)}");
            return 0;
        }

        try
        {
            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(postfixMethod));
            _logInfo?.Invoke($"[HarmonyReplayHooks] Hook aplicado em {targetType.Name}.{targetMethod.Name}");
            return 1;
        }
        catch (Exception ex)
        {
            _logWarn?.Invoke($"[HarmonyReplayHooks] Falha ao aplicar hook em {targetType.Name}.{targetMethod.Name}: {ex.Message}");
            return 0;
        }
    }

    public static Type? FindTypeByCandidates(IEnumerable<string> candidateNames)
    {
        var candidates = new HashSet<string>(candidateNames, StringComparer.Ordinal);
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly asm in assemblies)
        {
            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            }
            catch
            {
                continue;
            }

            foreach (Type type in types)
            {
                if (type is null)
                {
                    continue;
                }

                if (candidates.Contains(type.Name) || (!string.IsNullOrEmpty(type.FullName) && candidates.Contains(type.FullName!)))
                {
                    return type;
                }
            }
        }

        return null;
    }

    public static MethodInfo? FindMethodByCandidates(Type targetType, IEnumerable<string> methodNames)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        MethodInfo[] methods = targetType.GetMethods(flags);

        foreach (string candidate in methodNames)
        {
            MethodInfo? match = methods.FirstOrDefault(m => string.Equals(m.Name, candidate, StringComparison.Ordinal));
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static void OnMatchStartedPostfix(object? __instance)
    {
        string sourceName = __instance?.GetType().Name ?? "MatchController";
        string matchId = TryReadStringMember(__instance, "GameId", "MatchId", "RoomId") ?? "match";

        ActiveSession?.StartMatch(matchId, new Dictionary<string, string>
        {
            ["source"] = sourceName
        });
    }

    private static void OnMeetingStartedPostfix(object? __instance)
    {
        string actor = TryReadPlayerName(__instance) ?? "meeting";
        ActiveSession?.RecordEvent("meeting_start", actor, "Reunião de emergência/corpo iniciada", null);
    }

    private static void OnMurderPlayerPostfix(object? __instance, object?[]? __args)
    {
        string killer = TryReadPlayerName(__instance) ?? "unknown_killer";
        string? targetName = null;

        if (__args is { Length: > 0 } && __args[0] is object targetObj)
        {
            targetName = TryReadPlayerName(targetObj) ?? targetObj.GetType().Name;
        }

        var meta = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(targetName))
        {
            meta["target"] = targetName;
        }

        ActiveSession?.RecordEvent("player_murder", killer, $"Eliminação registrada: {killer} -> {targetName ?? "desconhecido"}", meta);
    }

    public static string? TryReadPlayerName(object? instance)
    {
        if (instance is null)
        {
            return null;
        }

        string? direct = TryReadStringMember(instance, "PlayerName", "Name", "Nickname");
        if (!string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        object? dataObj = TryReadObjectMember(instance, "Data", "PlayerData", "Outfits");
        if (dataObj is not null && !ReferenceEquals(dataObj, instance))
        {
            return TryReadStringMember(dataObj, "PlayerName", "Name");
        }

        return null;
    }

    public static string? TryReadStringMember(object? instance, params string[] memberNames)
    {
        object? val = TryReadObjectMember(instance, memberNames);
        return val?.ToString();
    }

    public static object? TryReadObjectMember(object? instance, params string[] memberNames)
    {
        if (instance is null)
        {
            return null;
        }

        Type type = instance.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (string name in memberNames)
        {
            PropertyInfo? prop = type.GetProperty(name, flags);
            if (prop is not null && prop.GetIndexParameters().Length == 0)
            {
                try
                {
                    return prop.GetValue(instance);
                }
                catch
                {
                    // Ignora falhas de acesso em runtime IL2CPP
                }
            }

            FieldInfo? field = type.GetField(name, flags);
            if (field is not null)
            {
                try
                {
                    return field.GetValue(instance);
                }
                catch
                {
                    // Ignora falhas de acesso em runtime IL2CPP
                }
            }
        }

        return null;
    }

    public static ReplayMatchSession? ActiveSession { get; set; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _harmony.UnpatchSelf();
        if (ReferenceEquals(ActiveSession, _session))
        {
            ActiveSession = null;
        }
    }
}
