// ReSharper disable InconsistentNaming,RedundantAssignment

using BrutalCompanyAdditions.Objects;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace BrutalCompanyAdditions.Patches;

public static class BCPatches {
    [HarmonyPatch(typeof(GameNetworkManager), "Start")]
    [HarmonyPostfix]
    private static void InjectNetworkManager() {
        NetworkManager.Singleton.AddNetworkPrefab(Plugin.BCNetworkManagerPrefab);
    }

    [HarmonyPatch(typeof(StartOfRound), "Awake")]
    [HarmonyPostfix]
    private static void SpawnNetworkManager() {
        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer) return;
        Object.Instantiate(Plugin.BCNetworkManagerPrefab, Vector3.zero, Quaternion.identity)
            .GetComponent<NetworkObject>().Spawn(); // spawn network manager
    }

    [HarmonyPatch(typeof(RoundManager), "DespawnPropsAtEndOfRound")]
    [HarmonyPostfix]
    private static void HandleCustomEventEnd(ref RoundManager __instance) {
        if (!__instance.IsHost) return;

        var eventId = Utils.LastEvent;
        BCNetworkManager.Instance.ClearCurrentEvent((int)eventId);

        if (!EventRegistry.IsCustomEvent(eventId)) return;
        var customEvent = EventRegistry.GetEvent(eventId);
        Plugin.Logger.LogWarning($"Ending custom event {customEvent.Name}... (server)");
        customEvent.OnEnd();
    }

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

        // Let the clients know about the event.
        BCNetworkManager.Instance.SetCurrentEvent((int)eventEnum, newLevel.levelID);

        return false;
    }

    // TODO: If I leave this in, I made a sincere oopsie.
    [HarmonyPatch(typeof(BrutalCompanyPlus.Plugin), "UpdateMapObjects")]
    [HarmonyPostfix]
    public static void DebugManyTurrets(ref SelectableLevel newLevel) {
        foreach (var mapObject in newLevel.spawnableMapObjects) {
            if (!mapObject.IsObjectTypeOf<Turret>(out _)) continue;
            mapObject.numberToSpawn = new AnimationCurve(
                new Keyframe(0.0f, 10f),
                new Keyframe(1f, 10f)
            );
        }
    }
}