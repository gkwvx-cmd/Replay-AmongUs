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

        installed += TryPatchMethod(
            typeNames: new[] { "EndGameManager", "AmongUsClient", "InnerNet.InnerNetClient" },
            methodNames: new[] { "Start", "OnGameEnd", "OnDisconnected" },
            postfixMethod: AccessTools.Method(typeof(HarmonyReplayHooks), nameof(OnMatchEndedPostfix))
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
        if (candidateNames is null)
        {
            return null;
        }

        var candidates = new HashSet<string>(
            candidateNames.Where(name => !string.IsNullOrWhiteSpace(name)),
            StringComparer.Ordinal
        );

        if (candidates.Count == 0)
        {
            return null;
        }

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly asm in assemblies)
        {
            string? asmName = asm.GetName().Name;
            if (IsSystemAssembly(asmName))
            {
                continue;
            }

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
                if (candidates.Contains(type.Name) || (!string.IsNullOrEmpty(type.FullName) && candidates.Contains(type.FullName)))
                {
                    return type;
                }
            }
        }

        return null;
    }

    private static bool IsSystemAssembly(string? assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName))
        {
            return false;
        }

        return assemblyName.Equals("System", StringComparison.Ordinal)
            || assemblyName.StartsWith("System.", StringComparison.Ordinal)
            || assemblyName.StartsWith("Microsoft.", StringComparison.Ordinal)
            || assemblyName.Equals("netstandard", StringComparison.Ordinal)
