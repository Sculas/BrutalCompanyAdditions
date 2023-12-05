using System.Collections.Generic;
using BepInEx.Configuration;

namespace BrutalCompanyAdditions;

public static class PluginConfig {
    public static ConfigEntry<bool> CustomOnly { get; private set; }
    public static Dictionary<string, ConfigEntry<bool>> EventConfig { get; } = new();

    public static void Bind(Plugin Plugin) {
        CustomOnly = Plugin.Config.Bind("General", "CustomOnly", false,
            "Whether to only use custom events or not.");
        foreach (var customEvent in EventRegistry.Events) {
            EventConfig[customEvent.Name] = Plugin.Config.Bind("Events", customEvent.Name, true,
                $"Whether the {customEvent.Name} event can be selected or not.");

            var enabled = EventConfig[customEvent.Name].Value;
            Plugin.Logger.LogInfo($"Event {customEvent.Name} is {(enabled ? "enabled" : "disabled")}.");
        }
    }
}