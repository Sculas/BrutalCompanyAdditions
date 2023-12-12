﻿// ReSharper disable InconsistentNaming,RedundantAssignment

using HarmonyLib;

namespace BrutalCompanyAdditions.Patches;

public static class BCPatches {
    private static BrutalCompanyPlus.BCP.EventEnum LastEvent = BrutalCompanyPlus.BCP.EventEnum.None;

    [HarmonyPatch(typeof(BrutalCompanyPlus.Plugin), "SelectRandomEvent")]
    [HarmonyPrefix]
    public static bool InjectCustomEvents(ref BrutalCompanyPlus.BCP.EventEnum __result) {
        Plugin.Logger.LogWarning("Injecting custom events...");

        switch (EventRegistry.SelectableEvents.Count) {
            case 0:
                __result = LastEvent = BrutalCompanyPlus.BCP.EventEnum.None;
                return false;
            case 1:
                __result = LastEvent = EventRegistry.SelectableEvents[0];
                return false;
        }

        BrutalCompanyPlus.BCP.EventEnum selectedEvent;
        do {
            var eventId = UnityEngine.Random.Range(0, EventRegistry.SelectableEvents.Count);
            selectedEvent = EventRegistry.SelectableEvents[eventId];
        } while (selectedEvent == LastEvent);

        if (EventRegistry.IsCustomEvent(selectedEvent)) {
            var customEvent = EventRegistry.GetEvent(selectedEvent);
            Plugin.Logger.LogWarning($"Selected custom event {customEvent.Name}");
        } else {
            Plugin.Logger.LogWarning($"Selected original event {selectedEvent}");
        }

        __result = LastEvent = selectedEvent;
        return false;
    }

    [HarmonyPatch(typeof(BrutalCompanyPlus.Plugin), "HandleEventSpecificAdjustments")]
    [HarmonyPrefix]
    public static bool InjectEventHandlers(ref BrutalCompanyPlus.BCP.EventEnum eventEnum, ref SelectableLevel newLevel) {
        // Would preferably inject after this check, but we have no other choice than to do it here as well.
        if (newLevel.sceneName == "CompanyBuilding") {
            Plugin.Logger.LogWarning("Landed at The Company Building, skipping...");
            eventEnum = BrutalCompanyPlus.BCP.EventEnum.None;
        }

        if (!EventRegistry.IsCustomEvent(eventEnum)) {
            Plugin.Logger.LogWarning($"Event {eventEnum} is not a custom event, skipping...");
            return true;
        }

        var selectedEvent = EventRegistry.GetEvent(eventEnum);
        Plugin.Logger.LogWarning($"Handling custom event {selectedEvent.Name}...");
        Utils.SendEventMessage(selectedEvent);
        selectedEvent.Execute(newLevel);
        return false;
    }
}