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
[BepInDependency("BrutalCompanyPlus", "3.3.0")]
public class Plugin : BaseUnityPlugin {
    private static bool _loaded;
    public new static ManualLogSource Logger;
    public static GameObject BCNetworkManagerPrefab;

    private void Awake() {
        Logger = base.Logger;
        PluginConfig.Bind(this);
        EventRegistry.Initialize();
        InitializeNetcode();
        InitializeBCNetworkManager();

        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(BCPatches));
        harmony.PatchAll(typeof(AIPatches));

        Logger.LogWarning("I'm alive! Time to rule the world >:]");
    }

    private void Start() => InitializeBCManager();
    private void OnDestroy() => InitializeBCManager();

    private static void InitializeBCManager() {
        if (_loaded) return;
        Logger.LogWarning($"Initializing {nameof(BCManager)}...");
        DontDestroyOnLoad(new GameObject(PluginInfo.PLUGIN_GUID, typeof(BCManager)));
        _loaded = true;
    }

    private static void InitializeBCNetworkManager() {
        Logger.LogWarning($"Initializing {nameof(BCNetworkManager)}...");
        var bundle =
            AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"{PluginInfo.PLUGIN_NAME}.Assets.brutalcompanyadditions"));
        BCNetworkManagerPrefab = bundle.LoadAsset<GameObject>("Assets/BCNetworkManager.prefab");
        BCNetworkManagerPrefab.AddComponent<BCNetworkManager>();
        bundle.Unload(false);
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