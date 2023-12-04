using BepInEx.Configuration;

namespace BrutalCompanyAdditions;

public static class PluginConfig {
    public static ConfigEntry<bool> CustomOnly { get; private set; }

    public static void Bind(Plugin Plugin) {
        CustomOnly = Plugin.Config.Bind("General", "CustomOnly", false,
            "Whether to only use custom events or not.");
    }
}