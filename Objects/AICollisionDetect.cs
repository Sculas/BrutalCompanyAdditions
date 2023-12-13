using UnityEngine;

namespace BrutalCompanyAdditions.Objects;

public class AICollisionDetect : MonoBehaviour {
    private const float DetectionRadius = 2.5f;

    private int _doorLayerMask;
    private Collider[] _doorColliders;

    public bool canOpenDoors = true;
    public float openDoorSpeedMultiplier = 1f;

    private void Start() {
        _doorLayerMask = LayerMask.GetMask("InteractableObject");
        _doorColliders = new Collider[8];
    }

    private void FixedUpdate() {
        // If the AI can't open doors, don't bother
        if (!canOpenDoors) return;
        // Find doors within <DetectionRadius> meters
        var numColliders = Physics.OverlapSphereNonAlloc(transform.position, DetectionRadius,
            _doorColliders, _doorLayerMask, QueryTriggerInteraction.Ignore);
        if (numColliders == 0) return;
        // For each door, check if it's a DoorLock
        for (var i = 0; i < numColliders; i++) {
            if (!_doorColliders[i].CompareTag("InteractTrigger")) continue;
            if (!_doorColliders[i].TryGetComponent(out DoorLock door)) continue;

            // It is, so invoke the same code as in DoorLock.OnTriggerStay
            if (door.isLocked || door.isDoorOpened) continue;
            
            door.enemyDoorMeter += Time.fixedDeltaTime * openDoorSpeedMultiplier;
            if (door.enemyDoorMeter <= 1f) continue;

            door.enemyDoorMeter = 0f;
            door.GetComponent<AnimatedObjectTrigger>()
                .TriggerAnimationNonPlayer(false, true);
            door.OpenDoorAsEnemyServerRpc();
        }
    }
}