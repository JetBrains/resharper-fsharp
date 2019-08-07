using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  [ProjectFileTypeDefinition(Name)]
  public class FSharpProjectFileType : KnownProjectFileType
  {
    public new const string Name = "F#";

    [CanBeNull, UsedImplicitly]
    public new static FSharpProjectFileType Instance { get; private set; }

    public const string FsExtension = ".fs";
    public const string MlExtension = ".ml";

    private FSharpProjectFileType()
      : base(Name, Name, new[] {FsExtension, MlExtension})
    {
    }

    protected FSharpProjectFileType(string name) : base(name)
    {
    }

    protected FSharpProjectFileType(string name, string presentableName) : base(name, presentableName)
    {
    }

    protected FSharpProjectFileType(string name, string presentableName, IEnumerable<string> extensions)
      : base(name, presentableName, extensions)
    {
    }

    public override BuildAction GetDefaultBuildAction(IProject project, string extension) => BuildAction.COMPILE;
  }
}
