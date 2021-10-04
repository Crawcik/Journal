using Flax.Build;

/// <inheritdoc />
public class JournalEditorTarget : GameProjectEditorTarget
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();
        Modules.Add("JournalEditor");
    }
}
