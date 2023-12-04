// ReSharper disable InconsistentNaming,RedundantAssignment

using System;
using HarmonyLib;

namespace BrutalCompanyAdditions.Patches;

public static class BCPatches {
    private static readonly int MinEventId = PluginConfig.CustomOnly.Value ? EventRegistry.OriginalEventCount : 0;
    private static BCP.Data.EventEnum LastEvent = BCP.Data.EventEnum.None;

    [HarmonyPatch(typeof(BrutalCompanyPlus.Plugin), "SelectRandomEvent")]
    [HarmonyPrefix]
    public static bool InjectCustomEvents(ref BCP.Data.EventEnum __result) {
        Plugin.Logger.LogWarning("Injecting custom events...");

        int eventId;
        do {
            eventId = UnityEngine.Random.Range(MinEventId, EventRegistry.EventCount);
        } while ((BCP.Data.EventEnum)eventId == LastEvent);

        if (EventRegistry.IsCustomEvent(eventId)) {
            var customEvent = EventRegistry.GetEvent(eventId);
            Plugin.Logger.LogWarning($"Selected custom event {customEvent.Name} ({eventId})");
        }
        else {
            var originalEvent = (BCP.Data.EventEnum)Enum.ToObject(typeof(BCP.Data.EventEnum), eventId);
            Plugin.Logger.LogWarning($"Selected original event {originalEvent} ({eventId})");
        }

        __result = LastEvent = (BCP.Data.EventEnum)eventId;
        return false;
    }

    [HarmonyPatch(typeof(BrutalCompanyPlus.Plugin), "HandleEventSpecificAdjustments")]
    [HarmonyPrefix]
    public static bool InjectEventHandlers(ref BCP.Data.EventEnum eventEnum, ref SelectableLevel newLevel) {
        // Would preferably inject after this check, but we have no other choice than to do it here as well.
        if (newLevel.sceneName == "CompanyBuilding") {
            Plugin.Logger.LogWarning("Landed at The Company Building, skipping...");
            eventEnum = BCP.Data.EventEnum.None;
        }

        var eventId = (int)eventEnum;
        if (!EventRegistry.IsCustomEvent(eventId)) {
            Plugin.Logger.LogWarning($"Event {eventEnum} ({eventId}) is not a custom event, skipping...");
            return true;
        }

        var selectedEvent = EventRegistry.GetEvent(eventId);
        Plugin.Logger.LogWarning($"Handling custom event {selectedEvent.Name} ({eventId})...");
        Utils.SendEventMessage(selectedEvent);
        selectedEvent.Execute(newLevel);
        return false;
    }
}