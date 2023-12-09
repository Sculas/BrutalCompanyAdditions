using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BrutalCompanyAdditions.Objects;
using BrutalCompanyAdditions.Patches;
using HarmonyLib;
using UnityEngine;

namespace BrutalCompanyAdditions;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInProcess("Lethal Company.exe")]
[BepInDependency("BrutalCompanyPlus", "3.0.0")]
public class Plugin : BaseUnityPlugin {
    private static bool _loaded;
    public new static ManualLogSource Logger;

    private void Awake() {
        Logger = base.Logger;
        PluginConfig.Bind(this);
        EventRegistry.Initialize();
        InitializeNetcode();

        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(BCPatches));

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} has loaded!");
    }

    private void OnDestroy() {
        if (_loaded) return;
        Logger.LogInfo("BCManager initialized!");
        DontDestroyOnLoad(new GameObject(PluginInfo.PLUGIN_GUID, typeof(BCManager)));
        _loaded = true;
    }

    private static void InitializeNetcode() {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes()) {
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods) {
                var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.IsEmpty()) continue;

                Logger.LogWarning($"Initializing RPCs for {type.Name}...");
                method.Invoke(null, null);
            }
        }
    }
}