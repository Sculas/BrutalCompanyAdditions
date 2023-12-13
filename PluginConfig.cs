using System.Collections.Generic;
using BepInEx.Configuration;
using BrutalCompanyAdditions.Objects;

namespace BrutalCompanyAdditions;

public static class PluginConfig {
    public static ConfigEntry<bool> CustomOnly { get; private set; }
    public static Dictionary<string, ConfigEntry<bool>> EventConfig { get; } = new();

    // Turret damage for MovingTurrets
    public static ConfigEntry<int> TurretDamage { get; private set; }

    // Debug config
    public static ConfigEntry<bool> DebugAI { get; private set; }
    public static ConfigEntry<bool> DebugAILogging { get; private set; }

    public static void Bind(Plugin Plugin) {
        CustomOnly = Plugin.Config.Bind("General", "CustomOnly", false,
            "Whether to only use custom events or not.");

        foreach (var customEvent in EventRegistry.Events) {
            EventConfig[customEvent.Name] = Plugin.Config.Bind("Events", customEvent.Name, true,
                $"{customEvent.Description} (default: enabled)");

            var enabled = EventConfig[customEvent.Name].Value;
            Plugin.Logger.LogInfo($"Event {customEvent.Name} is {(enabled ? "enabled" : "disabled")}.");
        }

        TurretDamage = Plugin.Config.Bind("Difficulty", "TurretDamage", MovingTurretAI.DefaultPlayerDamage,
            "Amount of damage a turret does to a player during the MovingTurrets event (vanilla: 50)");

        DebugAI = Plugin.Config.Bind("Advanced", "DebugAI", false,
            "Whether to show debug text above custom enemies or not (host only, can cause mild lag).");
        DebugAILogging = Plugin.Config.Bind("Advanced", "DebugAILogging", false,
            "Whether to enable logging for AI (can cause extreme lag).");
    }
}