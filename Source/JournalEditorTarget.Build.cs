using Flax.Build;

public class JournalEditorTarget : GameProjectEditorTarget
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();
        Modules.Add("JournalEditor");
    }
}
