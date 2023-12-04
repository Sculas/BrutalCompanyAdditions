using System;
using System.Collections.Generic;
using BrutalCompanyAdditions.Events;

namespace BrutalCompanyAdditions;

public static class EventRegistry {
    public static readonly int OriginalEventCount = Enum.GetValues(typeof(BCP.Data.EventEnum)).Length;
    public static int EventCount => OriginalEventCount + Events.Count;

    private static readonly List<IEvent> Events = new() {
        new BlingBling(),
        new TheVault()
    };

    public static bool IsCustomEvent(int EventId) => EventId >= OriginalEventCount;
    public static IEvent GetEvent(int EventId) => Events[EventId - OriginalEventCount];
}