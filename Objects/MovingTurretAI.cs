using System;
using System.Collections;
using System.Linq;
using GameNetcodeStuff;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace BrutalCompanyAdditions.Objects;

[RequireComponent(typeof(NavMeshAgent))]
public class MovingTurretAI : NetworkBehaviour {
    // speed at which the AI updates
    private const float AIIntervalTime = 0.2f; // 5 times per second

    // - Balancing
    private const float PlayerLostIntervalTime = 0.075f;

    // increases by PlayerLostIntervalTime every interval. PlayerLostMinTime/PlayerLostIntervalTime*AIIntervalTime = seconds until lost
    private const float PlayerLostMinTime = 1.5f; // 4 seconds

    // decreases by PlayerLostIntervalTime every interval. PlayerLostMaxTime/PlayerLostIntervalTime*AIIntervalTime = seconds until roaming
    private const float PlayerLostMaxTime = -5.0f; // 13 seconds

    // how fast the AI opens doors, until 1 second is reached
    private const float OpenDoorSpeedMultiplier = 0.5f; // 2 seconds

    // whether the AI can open doors while firing
    private const bool CanOpenDoorsWhileFiring = false;

    // agent speed and acceleration during different states
    private const float AngularSpeed = 150f; // rotation speed
    private const float RoamingSpeed = 1.5f;
    private const float RoamingAcceleration = 4f;
    private const float ChasingSpeed = 2.75f;
    private const float ChasingAcceleration = 8f;
    private const float FiringSpeed = 0f;
    private const float FiringAcceleration = 0f;

    // Sentinel values taken from CrawlerAI. Not sure where the actual value comes from.
    private const double RoamingCheckForPlayerIntervalTime = 0.05000000074505806;
    private const double ChasingCheckForPlayerIntervalTime = 0.07500000298023224;

    private const float RoamingRotationRange = 55f;
    private const float ChasingRotationRange = 65f;

    private const float UpdatePositionThreshold = 1f;
    private const float SyncMovementSpeed = 0.22f;

    // - Debug variables
    private bool _showDebugText;
    private TextMesh _debugText;
    private GameObject _debugTextObject;

    // - Sync variables
    private Vector3 _serverPosition;
    private Vector3 _tempVelocity;

    private float _previousYRotation;
    private float _targetYRotation;

    // - Cached components
    private NavMeshAgent _agent;
    private Turret _turret;
    private GameObject[] _aiNodes;
    private AICollisionDetect _collisionDetect;

    // - AI variables
    private double _updateAIInterval;
    private double _checkForPlayerInterval;
    private double _setDestinationToPlayerInterval;
    private AIState _aiState;
    private bool _hasTarget;
    private PlayerControllerB _targetedPlayer;
    private bool _lostPlayerInChase;
    private float _lastSeenTimer;
    private Vector3 _lastKnownPlayerPosition;
    private Collider[] _nearPlayerColliders;

    // - Pathfinding variables
    private Coroutine _searchCoroutine;
    private Coroutine _chooseTargetNodeCoroutine;
    private AISearchRoutine _currentSearch;
    private float _pathDistance;
    private NavMeshPath _path1;

    public override void OnDestroy() {
        base.OnDestroy();
        Destroy(_agent);
        if (_showDebugText) Destroy(_debugTextObject);
    }

    private void Start() {
        _serverPosition = transform.position;
        _agent = GetComponent<NavMeshAgent>();
        _turret = GetComponentInChildren<Turret>();

        if (!IsServer) { // client
            // Disable NavMeshAgent on clients
            _agent.enabled = false;
            return; // don't initialize AI on clients
        }

        // Add AI collision detection
        _collisionDetect = _turret.gameObject.AddComponent<AICollisionDetect>();
        _collisionDetect.openDoorSpeedMultiplier = OpenDoorSpeedMultiplier;

        // Values taken from Crawler
        _agent.stoppingDistance = 0.0f;
        _agent.angularSpeed = AngularSpeed;
        _agent.radius = 1.21f;
        _agent.height = 3.3f;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        _agent.avoidancePriority = 50;
        _agent.areaMask = 31; // exported from Unity as int

        // Initialize AI variables
        _aiNodes = GameObject.FindGameObjectsWithTag("AINode");
        _currentSearch = new AISearchRoutine();
        _path1 = new NavMeshPath();
        _nearPlayerColliders = new Collider[StartOfRound.Instance.allPlayerScripts.Length + 1];

        // Initialize debug variables
        _showDebugText = PluginConfig.DebugAI.Value && IsServer;
        if (_showDebugText) {
            _debugTextObject = new GameObject("BCA AI Debug Text") {
                transform = {
                    parent = _turret.aimPoint,
                    localPosition = new Vector3(0, 0.5f, 0),
                    localScale = new Vector3(0.5f, 0.5f, 0.5f),
                    rotation = Quaternion.Euler(0, 90, 0)
                }
            };
            _debugText = _debugTextObject.AddComponent<TextMesh>();
            _debugText.fontSize = 100;
            _debugText.anchor = TextAnchor.MiddleCenter;
        }

        // Start roaming
        StartRoaming();
    }

    private void Update() {
        if (StartOfRound.Instance.livingPlayers == 0) return;

        if (!IsServer) { // client
            // Sync position from server
            var currentTransform = transform;
            transform.position = Vector3.SmoothDamp(currentTransform.position, _serverPosition, ref _tempVelocity,
                SyncMovementSpeed);
            var eulerAngles = currentTransform.eulerAngles;
            eulerAngles = new Vector3(eulerAngles.x,
                Mathf.LerpAngle(eulerAngles.y, _targetYRotation, 15f * Time.deltaTime), eulerAngles.z);
            transform.eulerAngles = eulerAngles;
            return;
        }

        // Update debug text
        if (_showDebugText)
            _debugText.text = $"{_aiState} (lostPlayer = {_lostPlayerInChase}, hasTarget = {_hasTarget})";

        if (_hasTarget && _targetedPlayer != null) {
            if (_setDestinationToPlayerInterval <= 0.0) {
                _setDestinationToPlayerInterval = 0.25f;
                _agent.SetDestination(RoundManager.Instance.GetNavMeshPosition(_targetedPlayer.transform.position,
                    RoundManager.Instance.navHit, 2.7f));
            } else {
                var pos = _targetedPlayer.transform.position;
                _agent.SetDestination(new Vector3(pos.x, _agent.destination.y, pos.z));
                _setDestinationToPlayerInterval -= Time.deltaTime;
            }
        }

        // Disable AI when turret is disabled
        if (!_turret.turretActive) {
            _agent.enabled = false;
            return;
        }

        // Enable AI when turret is enabled
        _agent.enabled = true; // only invoked on server

        if (_updateAIInterval >= 0.0) {
            _updateAIInterval -= Time.deltaTime;
        } else {
            SyncPositionToClients();
            DoAIInterval();
            _updateAIInterval = AIIntervalTime;
        }

        if (Mathf.Abs(_previousYRotation - transform.eulerAngles.y) > 6.0) {
            _previousYRotation = transform.eulerAngles.y;
            _targetYRotation = _previousYRotation;
            UpdateEnemyRotationClientRpc((short)_previousYRotation);
        }

        PlayerControllerB player;
        switch (_aiState) {
            case AIState.Roaming:
                if (_checkForPlayerInterval <= RoamingCheckForPlayerIntervalTime) {
                    _checkForPlayerInterval += Time.deltaTime;
                    return;
                }

                // Reset interval
                _checkForPlayerInterval = 0.0f;

                if (!CheckLineOfSightForPlayer(out player, RoamingRotationRange, 3f, true))
                    break;

                // Player found, start chasing
                StartChasing(player);
                break;

            case AIState.Chasing:
                if (_turret.turretMode is TurretMode.Firing or TurretMode.Berserk) {
                    // Turret is firing, make them slow and unable to open doors (if applicable)
                    SetAgentSpeed(FiringSpeed, FiringAcceleration);
                    if (!CanOpenDoorsWhileFiring) _collisionDetect.canOpenDoors = false;
                } else {
                    // Turret is not firing, reset speed and allow opening doors (if applicable)
                    SetAgentSpeed(ChasingSpeed, ChasingAcceleration);
                    if (!CanOpenDoorsWhileFiring) _collisionDetect.canOpenDoors = true;
                }

                if (_checkForPlayerInterval <= ChasingCheckForPlayerIntervalTime) {
                    _checkForPlayerInterval += Time.deltaTime;
                    return;
                }

                // Reset interval
                _checkForPlayerInterval = 0.0f;

                // Did we lose the player?
                if (_lostPlayerInChase) {
                    // We did, try to find the player again
                    if (CheckLineOfSightForPlayer(out player, ChasingRotationRange, 3f)) {
                        // Player found, start chasing
                        _lastSeenTimer = 0.0f;
                        _lostPlayerInChase = false;
                        SetTargetedPlayer(player);
                        return;
                    }

                    // Player not found, try again for a couple of times
                    _lastSeenTimer -= PlayerLostIntervalTime;
                    if (_lastSeenTimer >= PlayerLostMaxTime) return;

                    // Player still not found after multiple attempts, start roaming
                    StartRoaming();
                    return;
                }

                // Player not lost, check if we can still see the player
                if (CheckLineOfSightForPlayer(out player, ChasingRotationRange, 3f)) {
                    // Player still in sight, update last seen timer
                    _lastSeenTimer = 0.0f;
                    _lastKnownPlayerPosition = player.transform.position;
                    SetTargetedPlayer(player);
                    return;
                }

                // Player lost in chase, update last seen timer
                _lastSeenTimer += PlayerLostIntervalTime;
                if (_lastSeenTimer <= PlayerLostMinTime) return;
                _lostPlayerInChase = true;

                break;

            default: throw new ArgumentOutOfRangeException();
        }
    }

    private void StartRoaming() {
        if (!SetAIState(AIState.Roaming)) return;
        SetTargetedPlayer(null);
        _checkForPlayerInterval = 0.0f;
        _lostPlayerInChase = false;
        _currentSearch.searchWidth = 25f;
        SetAgentSpeed(); // reset agent speed
    }

    private void StartChasing(PlayerControllerB Player) {
        if (!SetAIState(AIState.Chasing)) return;
        SetTargetedPlayer(Player);
        _checkForPlayerInterval = 0.0f;
        _lostPlayerInChase = false;
        SetAgentSpeed(ChasingSpeed, ChasingAcceleration);
    }

    private void DoAIInterval() {
        LogAI(() => "Starting AI interval");
        switch (_aiState) {
            case AIState.Roaming:
                AIRoaming();
                break;
            case AIState.Chasing:
                AIChasing();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void AIRoaming() {
        if (_currentSearch.inProgress) return;
        LogAI(() => "Starting player search");
        StartSearch(transform.position);
    }

    private void AIChasing() {
        CheckForVeryClosePlayer();
        if (_lostPlayerInChase) {
            if (_currentSearch.inProgress) return;
            _currentSearch.searchWidth = 30f;
            StartSearch(_lastKnownPlayerPosition);
            LogAI(() => "Lost player in chase, starting search at last known position");
            return;
        }

        if (!_currentSearch.inProgress) return;
        StopSearch(_currentSearch);
        LogAI(() => "Found player during chase, targeting player");
    }

    private void OnSearchFinished() {
        _currentSearch.searchWidth = Mathf.Clamp(_currentSearch.searchWidth + 10f, 1f, 100f);
    }

    public void StartSearch(Vector3 StartOfSearch) {
        StopSearch(_currentSearch);
        _currentSearch ??= new AISearchRoutine();

        _currentSearch.currentSearchStartPosition = StartOfSearch;
        if (_currentSearch.unsearchedNodes.IsEmpty()) _currentSearch.unsearchedNodes = _aiNodes.ToList();

        _searchCoroutine = StartCoroutine(PlayerSearchRoutine());
        _currentSearch.inProgress = true;
    }

    public void StopSearch(AISearchRoutine Search, bool Clear = true) {
        if (Search == null) return;
        if (_searchCoroutine != null) StopCoroutine(_searchCoroutine);
        if (_chooseTargetNodeCoroutine != null) StopCoroutine(_chooseTargetNodeCoroutine);

        Search.calculatingNodeInSearch = false;
        Search.inProgress = false;
        if (!Clear) return;

        Search.unsearchedNodes = _aiNodes.ToList();
        Search.timesFinishingSearch = 0;
        Search.nodesEliminatedInCurrentSearch = 0;
        Search.currentTargetNode = null;
        Search.currentSearchStartPosition = Vector3.zero;
        Search.nextTargetNode = null;
        Search.choseTargetNode = false;
    }

    // More or less a replica of EnemyAI#CurrentSearchRoutine.
    // I'm very sorry for the mess.
    private IEnumerator PlayerSearchRoutine() {
        yield return null;
        while (_searchCoroutine != null) {
            yield return null;
            if (_currentSearch.unsearchedNodes.IsEmpty()) {
                OnSearchFinished();

                if (!_currentSearch.loopSearch) {
                    _currentSearch.inProgress = false;
                    _currentSearch = null;
                    yield break;
                }

                _currentSearch.unsearchedNodes = _aiNodes.ToList();
                _currentSearch.timesFinishingSearch++;
                _currentSearch.nodesEliminatedInCurrentSearch = 0;
                yield return new WaitForSeconds(1f);
            }

            if (_currentSearch.choseTargetNode &&
                _currentSearch.unsearchedNodes.Contains(_currentSearch.nextTargetNode)) {
                _currentSearch.currentTargetNode = _currentSearch.nextTargetNode;
            } else {
                _currentSearch.waitingForTargetNode = true;
                StartCalculatingNextTargetNode();
                yield return new WaitUntil(() => _currentSearch.choseTargetNode);
            }

            _currentSearch.waitingForTargetNode = false;
            if (_currentSearch.unsearchedNodes.IsEmpty() ||
                _currentSearch.currentTargetNode == null) continue;
            _currentSearch.unsearchedNodes.Remove(_currentSearch.currentTargetNode);
            _agent.SetDestination(RoundManager.Instance.GetNavMeshPosition(
                _currentSearch.currentTargetNode.transform.position, RoundManager.Instance.navHit, -1f));

            for (var i = _currentSearch.unsearchedNodes.Count - 1; i >= 0; --i) {
                if (Vector3.Distance(_currentSearch.currentTargetNode.transform.position,
                        _currentSearch.unsearchedNodes[i].transform.position) <
                    (double)_currentSearch.searchPrecision)
                    EliminateNodeFromSearch(i);
                if (i % 10 == 0)
                    yield return null;
            }

            StartCalculatingNextTargetNode();
            var timeSpent = 0;
            while (_searchCoroutine != null) {
                if (++timeSpent < 32) {
                    yield return new WaitForSeconds(0.5f);
                    if (!(Vector3.Distance(transform.position,
                              _currentSearch.currentTargetNode.transform.position) <
                          (double)_currentSearch.searchPrecision)) continue;
                }

                break;
            }
        }
    }

    private void StartCalculatingNextTargetNode() {
        if (_chooseTargetNodeCoroutine == null) {
            _currentSearch.choseTargetNode = false;
            _chooseTargetNodeCoroutine = StartCoroutine(ChooseNextNodeInSearchRoutine());
        } else {
            if (_currentSearch.calculatingNodeInSearch) return;
            _currentSearch.choseTargetNode = false;
            _currentSearch.calculatingNodeInSearch = true;
            StopCoroutine(_chooseTargetNodeCoroutine);
            _chooseTargetNodeCoroutine = StartCoroutine(ChooseNextNodeInSearchRoutine());
        }
    }

    private IEnumerator ChooseNextNodeInSearchRoutine() {
        yield return null;
        var closestDist = 500f;
        var gotNode = false;
        GameObject chosenNode = null;

        for (var i = _currentSearch.unsearchedNodes.Count - 1; i >= 0; --i) {
            if (i % 5 == 0)
                yield return null;
            if (Vector3.Distance(_currentSearch.currentSearchStartPosition,
                    _currentSearch.unsearchedNodes[i].transform.position) >
                _currentSearch.searchWidth) {
                EliminateNodeFromSearch(i);
            } else if (PathIsIntersectedByLineOfSight(
                           _currentSearch.unsearchedNodes[i].transform.position,
                           true, false)
                      ) {
                EliminateNodeFromSearch(i);
            } else if (_pathDistance < closestDist && (
                           !_currentSearch.randomized ||
                           !gotNode ||
                           UnityEngine.Random.Range(0.0f, 100f) < 65.0)) {
                closestDist = _pathDistance;
                chosenNode = _currentSearch.unsearchedNodes[i];
                gotNode = true;
            }
        }

        if (_currentSearch.waitingForTargetNode)
            _currentSearch.currentTargetNode = chosenNode;
        else
            _currentSearch.nextTargetNode = chosenNode;

        _currentSearch.choseTargetNode = true;
        _currentSearch.calculatingNodeInSearch = false;
        _chooseTargetNodeCoroutine = null;
    }

    private bool PathIsIntersectedByLineOfSight(
        Vector3 TargetPos,
        bool CalculatePathDistance = false,
        bool AvoidLineOfSight = true) {
        _pathDistance = 0.0f;

        if (!_agent.CalculatePath(TargetPos, _path1))
            return true;

        if (Vector3.Distance(_path1.corners[_path1.corners.Length - 1],
                RoundManager.Instance.GetNavMeshPosition(TargetPos, RoundManager.Instance.navHit, 2.7f)) > 1.5)
            return true;

        if (CalculatePathDistance)
            for (var index = 1; index < _path1.corners.Length; ++index) {
                _pathDistance += Vector3.Distance(_path1.corners[index - 1], _path1.corners[index]);
                if (AvoidLineOfSight && Physics.Linecast(_path1.corners[index - 1], _path1.corners[index], 262144))
                    return true;
            }
        else if (AvoidLineOfSight)
            for (var index = 1; index < _path1.corners.Length; ++index)
                if (Physics.Linecast(_path1.corners[index - 1], _path1.corners[index], 262144))
                    return true;

        return false;
    }

    public void SyncPositionToClients() {
        if (Vector3.Distance(_serverPosition, transform.position) <= UpdatePositionThreshold) return;
        _serverPosition = transform.position;
        UpdateEnemyPositionClientRpc(_serverPosition);
    }

    [ClientRpc]
    private void UpdateEnemyPositionClientRpc(Vector3 NewPosition) {
        _serverPosition = NewPosition;
    }

    [ClientRpc]
    private void UpdateEnemyRotationClientRpc(short RotationY) {
        _previousYRotation = transform.eulerAngles.y;
        _targetYRotation = RotationY;
    }

    private void SetAgentSpeed(float Speed = RoamingSpeed, float Acceleration = RoamingAcceleration) {
        _agent.speed = Speed;
        _agent.acceleration = Acceleration;
    }

    private bool CheckLineOfSightForPlayer(out PlayerControllerB Player, float Width, float Radius = 2f,
        bool AngleRangeCheck = false) {
        _turret.rotationRange = Width;
        Player = _turret.CheckForPlayersInLineOfSight(Radius, AngleRangeCheck);
        return Player != null;
    }

    private void CheckForVeryClosePlayer() {
        // Find players within 1.5m
        if (Physics.OverlapSphereNonAlloc(transform.position, 1.5f, _nearPlayerColliders, 8,
                QueryTriggerInteraction.Ignore) <= 0) return;
        // Check if player is in range
        if (!_nearPlayerColliders[0].transform.TryGetComponent(out PlayerControllerB player)) return;
        // Check if player is not already targeted
        if (player == _targetedPlayer) return;
        // Check if player is in line of sight
        var intersected = Physics.Linecast(
            transform.position + Vector3.up * 0.3f,
            player.transform.position,
            StartOfRound.Instance.collidersAndRoomMask);
        if (intersected) return;
        // Start chasing player
        SetTargetedPlayer(player);
    }

    private void EliminateNodeFromSearch(int Index) {
        _currentSearch.unsearchedNodes.RemoveAt(Index);
        _currentSearch.nodesEliminatedInCurrentSearch++;
    }

    private bool SetAIState(AIState State) {
        if (_aiState == State) return false;
        _aiState = State;
        LogAI(() => $"Setting AI state to {State}");
        return true;
    }

    private void SetTargetedPlayer([CanBeNull] PlayerControllerB Player) {
        _targetedPlayer = Player;
        _hasTarget = Player != null;
        LogAI(() => $"Setting targeted player to {Player}");
    }

    private void LogAI(Func<string> Message) {
        if (!PluginConfig.DebugAILogging.Value) return;
        Plugin.Logger.LogInfo(
            $"[{nameof(MovingTurretAI)}] (state = {_aiState}) {Message()}"
        );
    }

    private enum AIState {
        Roaming,
        Chasing
    }
}