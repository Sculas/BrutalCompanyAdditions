using System.Collections.Generic;
using System.Linq;
using BrutalCompanyAdditions.Events;
using MonoMod.Utils;

namespace BrutalCompanyAdditions;

public static class EventRegistry {
    public static readonly List<IEvent> Events = new() {
        new BlingBling(),
        new TheVault(),
        new MovingTurrets()
    };

    private static readonly List<BrutalCompanyPlus.BCP.EventEnum> OriginalEvents =
        (from kvp in BrutalCompanyPlus.Plugin.eventWeightEntries
            where kvp.Value.Value > 0
            select kvp.Key).ToList();

    // Initialized below in [Initialize]
    public static readonly Dictionary<string, BrutalCompanyPlus.BCP.EventEnum> AllEvents = new();

    // This is the amount of events that Brutal Company Plus has by default.
    private static readonly int OriginalEventCount = BrutalCompanyPlus.Plugin.eventWeightEntries.Count;
    public static List<BrutalCompanyPlus.BCP.EventEnum> SelectableEvents;

    public static bool IsCustomEvent(BrutalCompanyPlus.BCP.EventEnum EventId) => (int)EventId >= OriginalEventCount;
    public static IEvent GetEvent(BrutalCompanyPlus.BCP.EventEnum EventId) => Events[(int)EventId - OriginalEventCount];
    public static IEvent GetEventByType<T>() where T : IEvent => Events.Find(Event => Event.GetType() == typeof(T));
    private static int GetEventId(IEvent Event) => OriginalEventCount + Events.IndexOf(Event);

    public static string GetEventName(BrutalCompanyPlus.BCP.EventEnum EventId) =>
        IsCustomEvent(EventId) ? GetEvent(EventId).Name : EventId.ToString();

    public static void Initialize() {
        var enabledEvents = (from kvp in PluginConfig.EventConfig
                where kvp.Value.Value
                select GetEventId(Events.Find(Event => Event.Name == kvp.Key)))
            .Cast<BrutalCompanyPlus.BCP.EventEnum>().ToList();

        SelectableEvents = new List<BrutalCompanyPlus.BCP.EventEnum>()
            .Concat(PluginConfig.CustomOnly.Value ? new BrutalCompanyPlus.BCP.EventEnum[] { } : OriginalEvents)
            .Concat(enabledEvents).ToList();

        AllEvents.AddRange(
            BrutalCompanyPlus.Plugin.eventWeightEntries.ToDictionary(
                Event => Event.Key.ToString(),
                Event => Event.Key
            )
        );
        AllEvents.AddRange(Events.ToDictionary(Event => Event.Name,
            Event => (BrutalCompanyPlus.BCP.EventEnum)GetEventId(Event)));
    }

    public static bool IsActive(this IEvent Event) => (int)Utils.LastEvent == GetEventId(Event);
}