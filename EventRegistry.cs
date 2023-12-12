using System.Collections.Generic;
using System.Linq;
using BrutalCompanyAdditions.Events;

namespace BrutalCompanyAdditions;

public static class EventRegistry {
    public static readonly List<IEvent> Events = new() {
        // TODO: If I forgot to remove these comments, I'm officially an idiot
        // new BlingBling(),
        // new TheVault(),
        new MovingTurrets()
    };

    private static readonly List<BrutalCompanyPlus.BCP.EventEnum> OriginalEvents =
        (from kvp in BrutalCompanyPlus.Plugin.eventConfigEntries
            where kvp.Value.Value
            select kvp.Key).ToList();

    // This is the amount of events that Brutal Company Plus has by default.
    private static readonly int OriginalEventCount = BrutalCompanyPlus.Plugin.eventConfigEntries.Count;
    public static List<BrutalCompanyPlus.BCP.EventEnum> SelectableEvents;

    public static bool IsCustomEvent(BrutalCompanyPlus.BCP.EventEnum EventId) => (int)EventId >= OriginalEventCount;
    public static IEvent GetEvent(BrutalCompanyPlus.BCP.EventEnum EventId) => Events[(int)EventId - OriginalEventCount];
    private static int GetEventId(IEvent Event) => OriginalEventCount + Events.IndexOf(Event);

    public static void Initialize() {
        var enabledEvents = (from kvp in PluginConfig.EventConfig
                where kvp.Value.Value
                select GetEventId(Events.Find(Event => Event.Name == kvp.Key)))
            .Cast<BrutalCompanyPlus.BCP.EventEnum>().ToList();
        SelectableEvents = new List<BrutalCompanyPlus.BCP.EventEnum>()
            .Concat(PluginConfig.CustomOnly.Value ? new BrutalCompanyPlus.BCP.EventEnum[] { } : OriginalEvents)
            .Concat(enabledEvents).ToList();
    }
}