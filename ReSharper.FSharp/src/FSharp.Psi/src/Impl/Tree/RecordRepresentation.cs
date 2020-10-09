using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class RecordRepresentation
  {
    public IList<ITypeOwner> GetFields()
    {
      var fieldDeclarations = FieldDeclarations;
      var result = new ITypeOwner[fieldDeclarations.Count];
      for (var i = 0; i < fieldDeclarations.Count; i++)
        if (fieldDeclarations[i].DeclaredElement is ITypeOwner field)
          result[i] = field;

      return result;
    }

    public IReadOnlyList<ITypeMemberDeclaration> GetMemberDeclarations() => 
      FieldDeclarations.AsIReadOnlyList();

    public override PartKind TypePartKind => TypeDeclaration.GetSimpleTypeKindFromAttributes();
  }
}
