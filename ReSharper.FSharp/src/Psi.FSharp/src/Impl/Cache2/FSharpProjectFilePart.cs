using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class FSharpProjectFilePart : SimpleProjectFilePart
  {
    public readonly int CacheVersion;

    public FSharpProjectFilePart(IPsiSourceFile sourceFile, int cacheVersion) : base(sourceFile)
    {
      CacheVersion = cacheVersion;
    }

    public FSharpProjectFilePart(IPsiSourceFile sourceFile, IReader reader) : base(sourceFile, reader)
    {
      CacheVersion = reader.ReadInt();
    }

    protected override void Write(IWriter writer)
    {
      writer.WriteInt(CacheVersion);
    }
  }
}