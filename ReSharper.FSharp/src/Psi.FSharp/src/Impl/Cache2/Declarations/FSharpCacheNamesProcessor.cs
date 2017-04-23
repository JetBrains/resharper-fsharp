using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations
{
  internal class FSharpCacheNamesProcessor : FSharpCacheDeclarationProcessorBase
  {
    private readonly FSharpNamesCacheBuilder myBuilder;

    internal FSharpCacheNamesProcessor(FSharpNamesCacheBuilder builder) : base(builder)
    {
      myBuilder = builder;
    }

    private void StartPart(IFSharpTypeElementDeclaration declaration, FSharpPartKind partKind)
    {
      myBuilder.StartTypePart(new FSharpTypeInfo(MakeClrName(declaration), partKind));
    }

    internal override void StartTypePart(IFSharpTypeElementDeclaration declaration, FSharpPartKind partKind)
    {
      StartPart(declaration, partKind);
    }
  }
}