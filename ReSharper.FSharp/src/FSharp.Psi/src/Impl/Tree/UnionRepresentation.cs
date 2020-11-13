using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class UnionRepresentation
  {
    private bool? myHasNestedTypes;

    protected override void ClearCachedData()
    {
      base.ClearCachedData();
      myHasNestedTypes = null;
    }

    public bool HasNestedTypes
    {
      get
      {
        if (myHasNestedTypes != null)
          return myHasNestedTypes.Value;

        lock (this)
          return myHasNestedTypes ??= TypePartKind != PartKind.Struct && UnionCases.Count > 1;
      }
    }

    public IReadOnlyList<ITypeMemberDeclaration> GetMemberDeclarations()
    {
      var result = new List<ITypeMemberDeclaration>();
      foreach (var unionCaseDecl in UnionCases)
      {
        result.Add(unionCaseDecl);
        if (!HasNestedTypes && unionCaseDecl.HasFields)
          result.AddRange(unionCaseDecl.Fields);
      }
      return result;
    }

    public override PartKind TypePartKind => TypeDeclaration.GetSimpleTypeKindFromAttributes();
  }
}
