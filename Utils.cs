using System.Collections.Generic;
using System.Linq;
using BrutalCompanyAdditions.Events;

namespace BrutalCompanyAdditions;

public static class Utils {
    public static void ReplaceScrap(this SelectableLevel Level, string ItemName, int Rarity, int MinValue, int MaxValue) {
        var oldMultiplier = RoundManager.Instance.scrapValueMultiplier;
        RoundManager.Instance.scrapValueMultiplier = 1f;
        
        var oldScrap = new List<SpawnableItemWithRarity>(Level.spawnableScrap);
        var newItem = UnityEngine.Object.Instantiate(FindItemByName(ItemName));
        newItem.minValue = MinValue; newItem.maxValue = MaxValue;

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

    private static Item FindItemByName(string ItemName) =>
        StartOfRound.Instance.allItemsList.itemsList.First(Item => Item.itemName == ItemName);

    public static void SendEventMessage(IEvent Event) {
        var positivity = Event.Positivity switch {
            EventPositivity.Positive => "green",
            EventPositivity.Neutral => "white",
            EventPositivity.Negative => "red",
            EventPositivity.Golden => "orange",
            _ => "white"
        };
        HUDManager.Instance.AddTextToChatOnServer(
            $"<color=yellow>EVENT<color=white>:</color></color>\n<color={positivity}>{Event.Name}</color>");
    }
}