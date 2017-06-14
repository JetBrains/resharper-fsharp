using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Searching;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  internal class ActivePatternCase : FSharpDeclaredElement<ActivePatternCaseDeclaration>, IFSharpSymbolElement
  {
    private readonly FSharpActivePatternCase myActivePatternCase;

    public ActivePatternCase(IFSharpDeclaration declaration, FSharpActivePatternCase activePatternCase)
      : base(declaration)
    {
      myActivePatternCase = activePatternCase;
    }

    public override DeclaredElementType GetElementType()
    {
      return FSharpDeclaredElementType.ActivePatternCase;
    }

    public override string ShortName => myActivePatternCase.Name;
    public FSharpSymbol Symbol => myActivePatternCase;
  }
}