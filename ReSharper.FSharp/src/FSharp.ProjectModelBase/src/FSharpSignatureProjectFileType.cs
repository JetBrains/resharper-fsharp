using JetBrains.Annotations;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  [ProjectFileTypeDefinition(Name)]
  public class FSharpSignatureProjectFileType : FSharpProjectFileType
  {
    public new const string Name = "F# Signature";

    [CanBeNull, UsedImplicitly]
    public new static FSharpSignatureProjectFileType Instance { get; private set; }

    public const string FsiExtension = ".fsi";
    public const string MliExtension = ".mli";

    private FSharpSignatureProjectFileType()
      : base(Name, Name, new[] {FsiExtension, MliExtension})
    {
    }

    protected FSharpSignatureProjectFileType(string name) : base(name)
    {
    }

    protected FSharpSignatureProjectFileType(string name, string presentableName) : base(name, presentableName)
    {
    }
  }
}
