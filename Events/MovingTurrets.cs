using BrutalCompanyAdditions.Objects;
using UnityEngine;

namespace BrutalCompanyAdditions.Events;

public class MovingTurrets : IEvent {
    public string Name => "Since when can they move?!";
    public string Description => "Turrets roam around the map, but have to be stationary to fire.";
    public EventPositivity Positivity => EventPositivity.Negative;

    private GameObject _turretPrefab;

    public void ExecuteServer(SelectableLevel Level) => Execute(Level);
    public void ExecuteClient(SelectableLevel Level) => Execute(Level);

    private void Execute(SelectableLevel Level) {
        foreach (var mapObject in Level.spawnableMapObjects) {
            if (!mapObject.IsObjectTypeOf<Turret>(out _)) continue;
            (_turretPrefab = mapObject.prefabToSpawn).AddComponent<MovingTurretAI>();
            break; // should only be one turret
        }
    }

    public void OnEnd() {
        if (_turretPrefab == null) return;
        var obj = _turretPrefab.GetComponent<MovingTurretAI>();
        if (obj != null) Object.Destroy(obj);
    }
}