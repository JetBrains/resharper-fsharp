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
  internal partial class UnionCaseFieldDeclaration
  {
    protected override string DeclaredElementName => Identifier.GetSourceName();
    public override IFSharpIdentifier NameIdentifier => Identifier;

    protected override IDeclaredElement CreateDeclaredElement()
    {
      // the field doesn't have a name and is in a union case or in an exception
      var typeDeclaration = Parent as IFSharpTypeDeclaration;
      var typeSymbol = typeDeclaration?.GetFSharpSymbol();
      if (typeSymbol == null)
        return null;

      var result = new LocalList<IUnionCaseFieldDeclaration>();
      foreach (var child in typeDeclaration.Children())
      {
        if (child is IUnionCaseFieldDeclaration fieldDeclaration)
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
        ? new FSharpUnionCaseField(this, caseField)
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
