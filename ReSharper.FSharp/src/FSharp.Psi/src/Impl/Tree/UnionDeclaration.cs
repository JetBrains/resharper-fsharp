using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class UnionDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(AllAttributes);
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;

    private bool? myHasNestedTypes;

    public bool HasNestedTypes
    {
      get
      {
        if (myHasNestedTypes != null)
          return myHasNestedTypes.Value;

        lock (this)
          return myHasNestedTypes ??= !this.HasAttribute(FSharpImplUtil.Struct) && UnionCases.Count > 1;
      }
    }

    public override IReadOnlyList<ITypeMemberDeclaration> MemberDeclarations
    {
      get
      {
        var result = new List<ITypeMemberDeclaration>();
        result.AddRange(base.MemberDeclarations);

        foreach (var unionCaseDecl in UnionCases)
        {
          result.Add(unionCaseDecl);
          if (!HasNestedTypes && unionCaseDecl is INestedTypeUnionCaseDeclaration decl)
            result.AddRange(decl.Fields);
        }

        return result;
      }
    }

    public override PartKind TypePartKind =>
      FSharpImplUtil.GetTypeKind(AllAttributes, out var typeKind)
        ? typeKind
        : PartKind.Class;
  }
}
