using JetBrains.Annotations;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  [ProjectFileTypeDefinition(Name)]
  public class FSharpScriptProjectFileType : FSharpProjectFileType
  {
    public new const string Name = "F# Script";
    [UsedImplicitly] public new static readonly FSharpScriptProjectFileType Instance;

    public const string FsxExtension = ".fsx";
    public const string FsScriptExtension = ".fsscript";

    private FSharpScriptProjectFileType()
      : base(Name, "F#", new[] {FsxExtension, FsScriptExtension})
    {
    }

    protected FSharpScriptProjectFileType(string name) : base(name)
    {
    }

    protected FSharpScriptProjectFileType(string name, string presentableName) : base(name, presentableName)
    {
    }

    public override BuildAction GetDefaultBuildAction(IProject project, string extension) => BuildAction.NONE;
  }
}
