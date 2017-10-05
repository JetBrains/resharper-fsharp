using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Searching;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class ActivePatternCase : FSharpDeclaredElement<ActivePatternCaseDeclaration>, IFSharpSymbolElement
  {
    private readonly FSharpActivePatternCase myActivePatternCase;

    public ActivePatternCase(IFSharpDeclaration declaration, FSharpActivePatternCase activePatternCase)
      : base(declaration)
    {
      myActivePatternCase = activePatternCase;
    }

    public override DeclaredElementType GetElementType() => FSharpDeclaredElementType.ActivePatternCase;
    public override string ShortName => myActivePatternCase.Name;
    public FSharpSymbol Symbol => myActivePatternCase;
  }
}