using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class TypeExtensionReference : FSharpSymbolReference
  {
    public TypeExtensionReference([NotNull] IReferenceExpression owner) : base(owner)
    {
    }

    public override FSharpSymbol GetFSharpSymbol() =>
      myOwner.IdentifierToken is var token && token != null
        ? myOwner.FSharpFile.GetSymbolDeclaration(token.GetTreeStartOffset().Offset)
        : null;
  }
}
