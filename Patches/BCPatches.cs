// ReSharper disable InconsistentNaming,RedundantAssignment

using HarmonyLib;
using UnityEngine;

namespace BrutalCompanyAdditions.Patches;

public static class BCPatches {
    // TODO: redo this. just sync it via a custom network object
    // [HarmonyPatch(typeof(RoundManager), "GenerateNewLevelClientRpc")]
    // [HarmonyPostfix]
    // public static void InjectCustomEventsClient(ref RoundManager __instance, int randomSeed) {
    //     if (__instance.IsServer) return;
    //     Plugin.Logger.LogWarning("Injecting custom events... (client)");
    //
    //     var selectedEvent = Utils.SelectRandomEvent(randomSeed);
    //     if (!EventRegistry.IsCustomEvent(selectedEvent)) return;
    //
    //     var customEvent = EventRegistry.GetEvent(selectedEvent);
    //     Plugin.Logger.LogWarning($"Handling custom event {customEvent.Name}... (client)");
    //     customEvent.ExecuteClient(__instance.currentLevel);
    // }

    [HarmonyPatch(typeof(BrutalCompanyPlus.Plugin), "SelectRandomEvent")]
    [HarmonyPrefix]
    public static bool InjectCustomEventsServer(ref BCP.Data.EventEnum __result) {
        Plugin.Logger.LogWarning("Injecting custom events... (server)");

        var selectedEvent = Utils.SelectRandomEvent();
        if (EventRegistry.IsCustomEvent(selectedEvent)) {
            var customEvent = EventRegistry.GetEvent(selectedEvent);
            Plugin.Logger.LogWarning($"Selected custom event {customEvent.Name}");
        } else {
            Plugin.Logger.LogWarning($"Selected original event {selectedEvent}");
        }

        __result = selectedEvent;
        return false;
    }

    [HarmonyPatch(typeof(BrutalCompanyPlus.Plugin), "HandleEventSpecificAdjustments")]
    [HarmonyPrefix]
    public static bool InjectEventHandlers(ref BCP.Data.EventEnum eventEnum, ref SelectableLevel newLevel) {
        // Would preferably inject after this check, but we have no other choice than to do it here as well.
        if (newLevel.sceneName == "CompanyBuilding") {
            Plugin.Logger.LogWarning("Landed at The Company Building, skipping...");
            eventEnum = BCP.Data.EventEnum.None;
            return true;
        }

        if (!EventRegistry.IsCustomEvent(eventEnum)) {
            Plugin.Logger.LogWarning($"Event {eventEnum} is not a custom event, skipping...");
            return true;
        }

        var selectedEvent = EventRegistry.GetEvent(eventEnum);
        Plugin.Logger.LogWarning($"Handling custom event {selectedEvent.Name}... (server)");
        Utils.SendEventMessage(selectedEvent);
        selectedEvent.ExecuteServer(newLevel);
        return false;
    }

    // TODO: If I leave this in, I made a sincere oopsie.
    [HarmonyPatch(typeof(BrutalCompanyPlus.Plugin), "UpdateMapObjects")]
    [HarmonyPostfix]
    public static void DebugManyTurrets(ref SelectableLevel newLevel) {
        foreach (var mapObject in newLevel.spawnableMapObjects) {
            if (!mapObject.IsObjectTypeOf<Turret>(out _)) continue;
            mapObject.numberToSpawn = new AnimationCurve(
                new Keyframe(0.0f, 15f),
                new Keyframe(1f, 15f)
            );
        }
    }
}