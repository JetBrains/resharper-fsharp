using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class RecordRepresentation
  {
    public IList<IFSharpFunctionalTypeField> GetFields()
    {
      var fieldDeclarations = FieldDeclarations;
      var result = new IFSharpFunctionalTypeField[fieldDeclarations.Count];
      for (var i = 0; i < fieldDeclarations.Count; i++)
        if (fieldDeclarations[i].DeclaredElement is IFSharpFunctionalTypeField field)
          result[i] = field;

      return result;
    }

    public IReadOnlyList<ITypeMemberDeclaration> GetMemberDeclarations() =>
      FieldDeclarations.AsIReadOnlyList();

    public override PartKind TypePartKind => TypeDeclaration.GetSimpleTypeKindFromAttributes();
  }
}
