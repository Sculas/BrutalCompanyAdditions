namespace BrutalCompanyAdditions.Events;

public interface IEvent {
    /// <summary>
    /// Name of the event.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Whether this event (and its effects) is positive, negative or neutral.
    /// </summary>
    public EventPositivity Positivity { get; }

    /// <summary>
    /// Execute the event.
    /// </summary>
    /// <param name="Level">the current level</param>
    public void Execute(SelectableLevel Level);
}

public enum EventPositivity {
    Positive,
    Neutral,
    Negative,
    Golden
}