using System;
using System.Collections.Generic;
using System.Linq;
using BrutalCompanyAdditions.Events;
using BrutalCompanyAdditions.Objects;

namespace BrutalCompanyAdditions;

public static class Utils {
    // const here breaks UnityNetcodeWeaver for some reason
    // ReSharper disable once ConvertToConstant.Local
    private static readonly BrutalCompanyPlus.BCP.EventEnum InvalidEvent = (BrutalCompanyPlus.BCP.EventEnum)(-1);

    public static BrutalCompanyPlus.BCP.EventEnum ForcedEvent = InvalidEvent;
    public static BrutalCompanyPlus.BCP.EventEnum LastEvent = BrutalCompanyPlus.BCP.EventEnum.None;

    public static bool IsEventForced => (int)ForcedEvent != (int)InvalidEvent;

    public static void ReplaceScrap(this SelectableLevel Level, string ItemName, int Rarity, int MinValue,
        int MaxValue) {
        var oldMultiplier = RoundManager.Instance.scrapValueMultiplier;
        RoundManager.Instance.scrapValueMultiplier = 1f;

        var oldScrap = new List<SpawnableItemWithRarity>(Level.spawnableScrap);
        var newItem = UnityEngine.Object.Instantiate(FindItemByName(ItemName));
        newItem.minValue = MinValue;
        newItem.maxValue = MaxValue;

        Level.spawnableScrap.Clear();
        Level.spawnableScrap.Add(new SpawnableItemWithRarity {
            rarity = Rarity,
            spawnableItem = newItem
        });

        BCManager.Instance.ExecuteAfterDelay(() => {
            RoundManager.Instance.scrapValueMultiplier = oldMultiplier;
            Level.spawnableScrap = oldScrap;
        }, 12f);
    }

    private static Item FindItemByName(string ItemName) {
        return StartOfRound.Instance.allItemsList.itemsList.First(Item => Item.itemName == ItemName);
    }

    public static bool IsObjectTypeOf<T>(this SpawnableMapObject MapObject, out T Component) {
        Component = MapObject.prefabToSpawn.GetComponentInChildren<T>();
        return Component != null;
    }

    public static BrutalCompanyPlus.BCP.EventEnum SelectRandomEvent() {
        if (ForcedEvent != InvalidEvent) {
            LastEvent = ForcedEvent;
            ForcedEvent = InvalidEvent;
            return LastEvent;
        }

        switch (EventRegistry.SelectableEvents.Count) {
            case 0:
                return LastEvent = BrutalCompanyPlus.BCP.EventEnum.None;
            case 1:
                return LastEvent = EventRegistry.SelectableEvents[0];
        }

        BrutalCompanyPlus.BCP.EventEnum selectedEvent;
        do {
            var eventId = UnityEngine.Random.Range(0, EventRegistry.SelectableEvents.Count);
            selectedEvent = EventRegistry.SelectableEvents[eventId];
            Plugin.Logger.LogWarning($"Selected event {eventId} ({selectedEvent}), last event was {LastEvent}");
        } while (selectedEvent == LastEvent);

        return LastEvent = selectedEvent;
    }

    public static bool TryFindEventByName(string Name, out BrutalCompanyPlus.BCP.EventEnum SelectedEvent) {
        SelectedEvent = EventRegistry.AllEvents.TryGetFirst(
            EventName => EventName.Key.StartsWith(Name, StringComparison.InvariantCultureIgnoreCase),
            out var found
        ).Value;
        return found;
    }

    public static void SendEventMessage(IEvent Event) {
        var positivity = Event.Positivity switch {
            EventPositivity.Positive => "green",
            EventPositivity.Neutral => "white",
            EventPositivity.Negative => "red",
            EventPositivity.Golden => "orange",
            _ => "white"
        };
        HUDManager.Instance.AddTextToChatOnServer(
            $"<color=yellow>EVENT<color=white>:</color></color>\n" +
            $"<color={positivity}>{Event.Name}</color>\n" +
            $"<color=white><size=70%>{Event.Description}</size></color>"
        );
    }

    public static bool IsEmpty<T>(this IEnumerable<T> Collection) {
        return !Collection.Any();
    }

    private static TSource TryGetFirst<TSource>(this IEnumerable<TSource> Source,
        Func<TSource, bool> Predicate, out bool Found) {
        foreach (var element in Source) {
            if (!Predicate(element)) continue;
            Found = true;
            return element;
        }

        Found = false;
        return default;
    }
}