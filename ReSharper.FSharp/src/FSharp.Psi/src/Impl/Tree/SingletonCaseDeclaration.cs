using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class SingletonCaseDeclaration
  {
    protected override string DeclaredElementName => Identifier.GetSourceName();
    public override IFSharpIdentifier NameIdentifier => Identifier;

    protected override IDeclaredElement CreateDeclaredElement() =>
      new FSharpUnionCaseProperty(this);
  }
}
