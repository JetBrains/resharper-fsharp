using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class DelegateDeclaration
  {
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);

    public FSharpEntity Delegate => GetFSharpSymbol() as FSharpEntity;
    public FSharpDelegateSignature DelegateSignature => Delegate.FSharpDelegateSignature;
  }
}
