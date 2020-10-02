using System;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NestedTypeUnionCaseDeclaration : ICachedTypeMemberDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetSourceName();
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;

    private static readonly Func<NestedTypeUnionCaseDeclaration, IDeclaredElement>
      DeclaredElementFactory = declaration => new FSharpHiddenUnionCaseProperty(declaration);

    public override IDeclaredElement DeclaredElement =>
      this.GetOrCreateDeclaredElement(DeclaredElementFactory);

    public FSharpNestedTypeUnionCase NestedType => 
      (FSharpNestedTypeUnionCase) CacheDeclaredElement;
  }
}
