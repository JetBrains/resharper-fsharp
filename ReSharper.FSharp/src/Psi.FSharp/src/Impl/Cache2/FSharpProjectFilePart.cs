using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class FSharpProjectFilePart : SimpleProjectFilePart
  {
    public FSharpProjectFilePart(IPsiSourceFile sourceFile) : base(sourceFile)
    {
    }

    public FSharpProjectFilePart(IPsiSourceFile sourceFile, IReader reader) : base(sourceFile, reader)
    {
    }

    protected override void Write(IWriter writer)
    {
    }
  }
}