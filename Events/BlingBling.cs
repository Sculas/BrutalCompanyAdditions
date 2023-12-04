namespace BrutalCompanyAdditions.Events;

public class BlingBling : IEvent {
    public string Name => "Bling bling";
    public string Description => "So many gold bars! ...right?";
    public EventPositivity Positivity => EventPositivity.Neutral;

    public void Execute(SelectableLevel Level) =>
        Level.ReplaceScrap("Gold bar", 100, 1, 10);
}