using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class FieldDeclaration
  {
    public override string DeclaredName => FSharpImplUtil.GetCompiledName(Identifier, Attributes);
    public override string SourceName => FSharpImplUtil.GetSourceName(Identifier);

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var symbol = GetFSharpSymbol();
      var unionCase = symbol as FSharpUnionCase;
      if (unionCase != null)
        return new FSharpUnionCaseProperty(this, unionCase);

      var field = symbol as FSharpField;
      if (field != null)
        return new FSharpFieldProperty(this, field);

      // the field doesn't have a name and is in a union case or in an exception
      var typeDeclaration = Parent as IFSharpTypeDeclaration;
      var typeSymbol = typeDeclaration?.GetFSharpSymbol();
      if (typeDeclaration == null || typeSymbol == null)
        return null;

      var fieldDeclarations = typeDeclaration.Children<FieldDeclaration>().ToTreeNodeCollection();
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
      var unionCase = typeSymbol as FSharpUnionCase;
      if (unionCase != null)
        return unionCase.UnionCaseFields;

      var entity = typeSymbol as FSharpEntity;
      if (entity != null && entity.IsFSharpExceptionDeclaration)
        return entity.FSharpFields;

      return null;
    }
  }
}