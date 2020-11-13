using System;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class UnionCaseDeclaration : ICachedTypeMemberDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetSourceName();
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;

    private bool? myHasFields;

    protected override void ClearCachedData()
    {
      base.ClearCachedData();
      myHasFields = null;
    }

    private static readonly Func<UnionCaseDeclaration, IDeclaredElement>
      DeclaredElementFactory = declaration => new FSharpUnionCaseProperty(declaration);

    public override IDeclaredElement DeclaredElement =>
      this.GetOrCreateDeclaredElement(DeclaredElementFactory);

    public bool HasFields
    {
      get
      {
        if (myHasFields != null)
          return myHasFields.Value;

        lock (this)
          return myHasFields ??= !FieldsEnumerable.IsEmpty();
      }
    }

    public FSharpUnionCaseClass NestedType =>
      (FSharpUnionCaseClass) CacheDeclaredElement;
  }
}
