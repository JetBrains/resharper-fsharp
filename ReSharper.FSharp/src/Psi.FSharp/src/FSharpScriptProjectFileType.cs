using JetBrains.Annotations;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Psi.FSharp
{
  [ProjectFileTypeDefinition(Name)]
  public class FSharpScriptProjectFileType : KnownProjectFileType
  {
    public new const string Name = "F# script";
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
  }
}