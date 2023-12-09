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

    private static readonly List<BCP.Data.EventEnum> OriginalEvents =
        (from kvp in BrutalCompanyPlus.Plugin.eventConfigEntries
            where kvp.Value.Value
            select kvp.Key).ToList();

    // This is the amount of events that Brutal Company Plus has by default.
    private static readonly int OriginalEventCount = BrutalCompanyPlus.Plugin.eventConfigEntries.Count;
    public static List<BCP.Data.EventEnum> SelectableEvents;

    public static bool IsCustomEvent(BCP.Data.EventEnum EventId) => (int)EventId >= OriginalEventCount;
    public static IEvent GetEvent(BCP.Data.EventEnum EventId) => Events[(int)EventId - OriginalEventCount];
    private static int GetEventId(IEvent Event) => OriginalEventCount + Events.IndexOf(Event);

    public static void Initialize() {
        var enabledEvents = (from kvp in PluginConfig.EventConfig
                where kvp.Value.Value
                select GetEventId(Events.Find(Event => Event.Name == kvp.Key)))
            .Cast<BCP.Data.EventEnum>().ToList();
        SelectableEvents = new List<BCP.Data.EventEnum>()
            .Concat(PluginConfig.CustomOnly.Value ? new BCP.Data.EventEnum[] { } : OriginalEvents)
            .Concat(enabledEvents).ToList();
    }
}