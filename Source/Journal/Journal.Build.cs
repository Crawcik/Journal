using Flax.Build;
using Flax.Build.NativeCpp;

public class Journal : GameModule
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();
        BuildNativeCode = false;
    }

    /// <inheritdoc />
    public override void Setup(BuildOptions options)
    {
        base.Setup(options);
        options.ScriptingAPI.IgnoreMissingDocumentationWarnings = true;
    }
}
