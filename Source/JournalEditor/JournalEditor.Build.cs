using Flax.Build;
using Flax.Build.NativeCpp;

public class JournalEditor : GameEditorModule
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
        options.PublicDependencies.Add("Journal");
        options.ScriptingAPI.IgnoreMissingDocumentationWarnings = true;
    }
}
