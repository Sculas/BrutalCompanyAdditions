// ReSharper disable InconsistentNaming,RedundantAssignment

using HarmonyLib;
using UnityEngine;

namespace BrutalCompanyAdditions.Patches;

[HarmonyPatch(typeof(Terminal))]
public class TerminalPatches {
    private const string ForceEventUsage = "Usage: forceevent [event name, partial allowed]";
    private static readonly string AllEventNames = $"Available events: {EventRegistry.AllEvents.Keys.Join()}";

    [HarmonyPostfix, HarmonyPatch("ParsePlayerSentence")]
    public static void BCTerminalParser(ref Terminal __instance, ref TerminalNode __result) {
        var (keyword, args) = ParseCommand(__instance.screenText.text[^__instance.textAdded..]);
        switch (keyword) {
            case "forceevent" when __instance.IsHost: {
                if (string.IsNullOrWhiteSpace(args)) {
                    var currentEvent = Utils.IsEventForced
                        ? $"Currently forced event: {EventRegistry.GetEventName(Utils.ForcedEvent)}"
                        : "No event is currently forced.";
                    Respond(out __result, $"{currentEvent}\n\n{ForceEventUsage}\n\n{AllEventNames}");
                    return;
                }

                if (!Utils.TryFindEventByName(args, out var selectedEvent)) {
                    Respond(out __result, $"Invalid event name: {args}\n\n{ForceEventUsage}\n\n{AllEventNames}");
                    return;
                }

                Utils.ForcedEvent = selectedEvent;
                Respond(out __result, $"Forced next event to: {EventRegistry.GetEventName(selectedEvent)}");
                break;
            }
            case "forceevent" when !__instance.IsHost:
                Respond(out __result, "You must be the host to use this command!");
                break;
        }
    }

    private static (string, string) ParseCommand(string command) {
        var split = command.ToLower().Split(' ');
        return (split[0], split[1..].Join(delimiter: " "));
    }

    private static void Respond(out TerminalNode node, string text, bool clearPreviousText = true) {
        node = ScriptableObject.CreateInstance<TerminalNode>();
        node.displayText = text + "\n\n\n";
        node.clearPreviousText = clearPreviousText;
    }
}