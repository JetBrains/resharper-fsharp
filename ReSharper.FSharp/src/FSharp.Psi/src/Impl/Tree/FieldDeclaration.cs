using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class FieldDeclaration
  {
    protected override string DeclaredElementName => Identifier.GetCompiledName(Attributes);
    public override string SourceName => Identifier.GetSourceName();
    public override TreeTextRange GetNameRange() => Identifier.GetNameRange();

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var symbol = GetFSharpSymbol();
      if (symbol is FSharpField field)
        return new FSharpFieldProperty(this, field);

      // the field doesn't have a name and is in a union case or in an exception
      var typeDeclaration = Parent as IFSharpTypeDeclaration;
      var typeSymbol = typeDeclaration?.GetFSharpSymbol();
      if (typeDeclaration == null || typeSymbol == null)
        return null;

      var result = new LocalList<IFieldDeclaration>();
      foreach (var child in typeDeclaration.Children())
      {
        if (child is IFieldDeclaration fieldDeclaration)
          result.Add(fieldDeclaration);
      }

      var fieldDeclarations = result.ReadOnlyList();
      var index = fieldDeclarations.IndexOf(this);
      var fields = GetFields(typeSymbol);
      var caseField =
        fields != null && index <= fields.Count
          ? fields[index]
          : null;

      return caseField != null
        ? new FSharpFieldProperty(this, caseField)
        : null;
    }

    private static IList<FSharpField> GetFields([NotNull] FSharpSymbol typeSymbol)
    {
      switch (typeSymbol)
      {
        case FSharpUnionCase unionCase:
          return unionCase.UnionCaseFields;
        case FSharpEntity entity when entity.IsFSharpExceptionDeclaration:
          return entity.FSharpFields;
        default:
          return null;
      }
    }
  }
}