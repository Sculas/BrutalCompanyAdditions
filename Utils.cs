using System.Collections.Generic;
using System.Linq;
using BrutalCompanyAdditions.Events;
using BrutalCompanyAdditions.Objects;

namespace BrutalCompanyAdditions;

public static class Utils {
    private static BCP.Data.EventEnum _lastEvent = BCP.Data.EventEnum.None;

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

    public static BCP.Data.EventEnum SelectRandomEvent() {
        switch (EventRegistry.SelectableEvents.Count) {
            case 0:
                _lastEvent = BCP.Data.EventEnum.None;
                return _lastEvent;
            case 1:
                _lastEvent = EventRegistry.SelectableEvents[0];
                return _lastEvent;
        }

        BCP.Data.EventEnum selectedEvent;
        do {
            var eventId = UnityEngine.Random.Range(0, EventRegistry.SelectableEvents.Count);
            selectedEvent = EventRegistry.SelectableEvents[eventId];
            Plugin.Logger.LogWarning($"Selected event {eventId} ({selectedEvent}), last event was {_lastEvent}");
        } while (selectedEvent == _lastEvent);

        return _lastEvent = selectedEvent;
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
}