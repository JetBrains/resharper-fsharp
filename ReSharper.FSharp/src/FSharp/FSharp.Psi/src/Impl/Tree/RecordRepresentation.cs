using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class RecordRepresentation
  {
    public IList<IFSharpFunctionalTypeField> GetFields()
    {
      var memberDeclarations = MemberDeclarations;
      var result = new List<IFSharpFunctionalTypeField>(memberDeclarations.Count);
      var nameIndices = new Dictionary<string, int>();

      foreach (var memberDecl in memberDeclarations)
      {
        switch (memberDecl)
        {
          case IRecordFieldDeclaration { DeclaredElement: IFSharpRecordField field }:
            AddField(field);
            break;
          case IDeclarationTypeSpread typeSpread:
            foreach (var field in typeSpread.GetFields())
              AddField(field);
            break;
        }
      }

      return result.WhereNotNull().AsIList();

      void AddField(IFSharpRecordField field)
      {
        var name = field.ShortName;
        if (nameIndices.TryGetValue(name, out var index))
        {
          if (result[index] is IFSharpRecordField { IsOwnField: true })
            return;

          result[index] = null;
        }

        nameIndices[name] = result.Count;
        result.Add(field);
      }
    }

    public IReadOnlyList<ITypeMemberDeclaration> GetMemberDeclarations() =>
      MemberDeclarations.OfType<ITypeMemberDeclaration>().AsIReadOnlyList();

    public override PartKind TypePartKind => TypeDeclaration.GetSimpleTypeKindFromAttributes();
  }
}
