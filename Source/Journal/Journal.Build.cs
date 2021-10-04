using Flax.Build;

/// <inheritdoc />
public class Journal : GameModule
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();
        BuildNativeCode = false;
    }
}
