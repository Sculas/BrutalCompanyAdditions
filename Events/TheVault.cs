namespace BrutalCompanyAdditions.Events;

public class TheVault : IEvent {
    public string Name => "Got any stock?";
    public string Description => "They used to say Fort Knox was buried here...";
    public EventPositivity Positivity => EventPositivity.Golden;

    public void ExecuteServer(SelectableLevel Level) {
        Level.ReplaceScrap("Gold bar", 100, 100, 350);
    }

    public void ExecuteClient(SelectableLevel Level) { }

    public void OnEnd() { }
}