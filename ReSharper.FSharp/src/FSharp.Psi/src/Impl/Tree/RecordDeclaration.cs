using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class RecordDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(AllAttributes);
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;

    public IList<ITypeOwner> GetFields()
    {
      var fieldDeclarations = FieldDeclarations;
      var result = new ITypeOwner[fieldDeclarations.Count];
      for (var i = 0; i < fieldDeclarations.Count; i++)
        if (fieldDeclarations[i].DeclaredElement is ITypeOwner field)
          result[i] = field;

      return result;
    }

    public override PartKind TypePartKind =>
      FSharpImplUtil.GetTypeKind(AllAttributes, out var typeKind)
        ? typeKind
        : PartKind.Class;

    public override IReadOnlyList<ITypeMemberDeclaration> MemberDeclarations =>
      base.MemberDeclarations.Prepend(FieldDeclarations).AsIReadOnlyList();
  }
}
