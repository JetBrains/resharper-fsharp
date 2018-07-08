using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
{
  [ProjectFileTypeDefinition(Name)]
  public class FSharpProjectFileType : KnownProjectFileType
  {
    public new const string Name = "F#";
    [UsedImplicitly] public new static readonly FSharpProjectFileType Instance;

    public const string FsExtension = ".fs";
    public const string FsiExtension = ".fsi";
    public const string MlExtension = ".ml";
    public const string MliExtension = ".mli";


    private FSharpProjectFileType()
      : base(Name, "F#", new[] {FsExtension, FsiExtension, MlExtension, MliExtension})
    {
    }

    protected FSharpProjectFileType(string name) : base(name)
    {
    }

    protected FSharpProjectFileType(string name, string presentableName) : base(name, presentableName)
    {
    }

    protected FSharpProjectFileType(string name, string presentableName, IEnumerable<string> extensions)
      : base(name, presentableName, extensions) { }
    
    public override BuildAction DefaultBuildAction => BuildAction.COMPILE;
  }
}