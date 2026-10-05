using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

internal partial class DeclarationTypeSpread
{
  private readonly CachedPsiValue<IList<IFSharpRecordField>> myFields = new();

  public IDeclaredType GetRecordType()
  {
    if (TypeName?.Reference.GetFcsSymbol() is not FSharpEntity entity || !entity.GetAbbreviatedEntity().IsFSharpRecord)
      return null;

    var fcsType = FSharpSymbolUtil.FcsEntityInstanceModule.OfEntity(entity).FcsType.GetAbbreviatedType();
    return fcsType.MapType(this) as IDeclaredType;
  }

  public IEnumerable<IFSharpRecordField> GetFields()
  {
    if (myFields.GetValue(this) is { } fields)
      return fields;

    myFields.SetValue(this, EmptyList<IFSharpRecordField>.Instance);
    fields = CalculateFields();
    myFields.SetValue(this, fields);
    return fields;
  }

  private IList<IFSharpRecordField> CalculateFields()
  {
    var representation = RecordRepresentationNavigator.GetByMemberDeclaration(this);
    if (representation?.TypeDeclaration is not ITypeDeclaration { DeclaredElement: { } containingType })
      return EmptyList<IFSharpRecordField>.Instance;

    var recordType = GetRecordType();
    if (recordType?.GetTypeElement() is not { } recordTypeElement)
      return EmptyList<IFSharpRecordField>.Instance;

    var substitution = recordType.GetSubstitution();
    return recordTypeElement.GetRecordFields()
      .Select(IFSharpRecordField (field) => new FSharpRecordFieldFromSpread(containingType, field, substitution))
      .ToList();
  }
}
