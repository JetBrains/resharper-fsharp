using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpProjectFilePart : SimpleProjectFilePart
  {
    public FSharpFileKind FileKind { get; }
    public bool HasPairFile { get; } // todo: rename HasPairFile to something better

    public FSharpProjectFilePart(IPsiSourceFile sourceFile, FSharpFileKind fileKind, bool hasPairFile)
      : base(sourceFile)
    {
      FileKind = fileKind;
      HasPairFile = hasPairFile;
    }

    public FSharpProjectFilePart(IPsiSourceFile sourceFile, IReader reader, FSharpFileKind fileKind, bool hasPairFile)
      : base(sourceFile, reader)
    {
      FileKind = fileKind;
      HasPairFile = hasPairFile;
    }

    protected override void Write(IWriter writer)
    {
    }

    public bool IsSignaturePart => FileKind == FSharpFileKind.SigFile;
    public bool IsImplementationPart => FileKind == FSharpFileKind.ImplFile;

    public override string ToString() => $"{GetType().Name}:{FileKind}";
  }
}