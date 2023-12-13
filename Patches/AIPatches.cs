// ReSharper disable InconsistentNaming,RedundantAssignment

using System.Collections.Generic;
using System.Reflection.Emit;
using BrutalCompanyAdditions.Events;
using BrutalCompanyAdditions.Objects;
using GameNetcodeStuff;
using HarmonyLib;

namespace BrutalCompanyAdditions.Patches;

public static class AIPatches {
    private static bool _patchFailed;

    [HarmonyPatch(typeof(MenuManager), "Start")]
    [HarmonyPrefix]
    public static void ShowPatchFailedMessage(MenuManager __instance) {
        if (__instance.isInitScene || !_patchFailed) return;
        __instance.DisplayMenuNotification(
            $"[{PluginInfo.PLUGIN_NAME}]\n\nA patch failed to apply. Report this issue together with your log file.",
            "[ Back ]");
        _patchFailed = false; // show the message only once
    }

    [HarmonyPatch(typeof(Turret), "Update")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TurretPlayerDamagePatch(IEnumerable<CodeInstruction> instructions) {
        const int patchesRequired = 2;
        var healthField = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.health));
        var controllerField =
            AccessTools.Field(typeof(GameNetworkManager), nameof(GameNetworkManager.localPlayerController));

        var modified = 0;
        CodeInstruction lastInstruction = null;
        foreach (var inst in instructions) {
            var valid = lastInstruction != null && modified < patchesRequired;
            if (valid && inst.opcode == OpCodes.Ldc_I4_S && lastInstruction.opcode == OpCodes.Ldfld &&
                (lastInstruction.OperandIs(healthField) || lastInstruction.OperandIs(controllerField))) {
                Plugin.Logger.LogInfo($"Patching turret damage; m = {modified}, c = {inst}, l = {lastInstruction}");

                modified++;
                lastInstruction = null; // next instruction is going to be the call anyway, so fast fail [valid] above
                yield return inst; // return the original instruction first for [original] parameter
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(AIPatches), nameof(GetTurretDamage)));
                continue;
            }

            lastInstruction = inst;
            yield return inst;
        }

        if (modified != patchesRequired) {
            Plugin.Logger.LogError(
                $"Failed to patch turret damage! Please report this issue. ({modified} != {patchesRequired})");
            _patchFailed = true;
        } else {
            Plugin.Logger.LogWarning("Turret damage patched successfully!");
        }
    }

    public static int GetTurretDamage(int original) => EventRegistry.GetEventByType<MovingTurrets>().IsActive()
        ? BCNetworkManager.Instance.TurretDamage.Value
        : original;
}