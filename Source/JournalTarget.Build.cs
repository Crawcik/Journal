using Flax.Build;

public class JournalTarget : GameProjectTarget
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();
        Modules.Add("Journal");
    }
}
