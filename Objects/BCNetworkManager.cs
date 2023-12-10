using Unity.Netcode;

namespace BrutalCompanyAdditions.Objects;

public class BCNetworkManager : NetworkBehaviour {
    private const string Tag = $"[{nameof(BCNetworkManager)}]";

    public static BCNetworkManager Instance { get; private set; }

    // current event id (BCP.Data.EventEnum as int)
    public NetworkVariable<CurrentNetEvent> CurrentEvent = new();

    private void Awake() {
        Instance = this;
        Log($"{nameof(BCNetworkManager)} initialized!");

        CurrentEvent.OnValueChanged += OnCurrentEventChanged;
    }

    public void SetCurrentEvent(int EventId, int LevelId) {
        CurrentEvent.Value = new CurrentNetEvent { EventId = EventId, LevelId = LevelId, IsEnding = false };
    }

    public void ClearCurrentEvent(int EventId) {
        var levelId = RoundManager.Instance.currentLevel.levelID;
        CurrentEvent.Value = new CurrentNetEvent { EventId = EventId, LevelId = levelId, IsEnding = true };
    }

    private void OnCurrentEventChanged(CurrentNetEvent OldEvent, CurrentNetEvent NewEvent) {
        if (IsServer || OldEvent.Equals(NewEvent)) return;
        Log($"Received new event: {NewEvent}, old event was {OldEvent}");
        Log("Injecting custom events... (client)");

        var selectedEvent = (BCP.Data.EventEnum)NewEvent.EventId;
        if (!EventRegistry.IsCustomEvent(selectedEvent)) return;
        var customEvent = EventRegistry.GetEvent(selectedEvent);

        if (NewEvent.IsEnding) {
            Log($"Ending custom event {customEvent.Name}... (client)");
            customEvent.OnEnd();
        } else {
            Log($"Handling custom event {customEvent.Name}... (client)");
            customEvent.ExecuteClient(StartOfRound.Instance.levels[NewEvent.LevelId]);
        }
    }

    private static void Log(string Message) => Plugin.Logger.LogWarning($"{Tag} {Message}");
}

public struct CurrentNetEvent : INetworkSerializable, System.IEquatable<CurrentNetEvent> {
    public int EventId;
    public int LevelId;
    public bool IsEnding;

    public CurrentNetEvent(int EventId, int LevelId, bool IsEnding) {
        this.EventId = EventId;
        this.LevelId = LevelId;
        this.IsEnding = IsEnding;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> Serializer) where T : IReaderWriter {
        if (Serializer.IsReader) {
            var reader = Serializer.GetFastBufferReader();
            reader.ReadValueSafe(out EventId);
            reader.ReadValueSafe(out LevelId);
            reader.ReadValueSafe(out IsEnding);
        } else {
            var writer = Serializer.GetFastBufferWriter();
            writer.WriteValueSafe(EventId);
            writer.WriteValueSafe(LevelId);
            writer.WriteValueSafe(IsEnding);
        }
    }

    public bool Equals(CurrentNetEvent Other) {
        return EventId == Other.EventId && LevelId == Other.LevelId && IsEnding == Other.IsEnding;
    }

    public override string ToString() => $"(eid: {EventId}, lid: {LevelId}, ending: {IsEnding})";
}