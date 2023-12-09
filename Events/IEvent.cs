namespace BrutalCompanyAdditions.Events;

public interface IEvent {
    /// <summary>
    /// Name of the event.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Description of the event.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Whether this event (and its effects) is positive, negative or neutral.
    /// </summary>
    public EventPositivity Positivity { get; }

    /// <summary>
    /// Execute the event on the server.
    /// </summary>
    /// <param name="Level">the current level</param>
    public void ExecuteServer(SelectableLevel Level);

    /// <summary>
    /// Execute the event on the client.
    /// </summary>
    /// <param name="Level">the current level</param>
    public void ExecuteClient(SelectableLevel Level);

    /// <summary>
    /// Called when the event is over.
    /// </summary>
    public void OnEnd();
}

public enum EventPositivity {
    Positive,
    Neutral,
    Negative,
    Golden
}