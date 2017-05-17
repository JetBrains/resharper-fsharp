using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
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
      {
        return new FSharpFieldProperty(this, unionCase);
      }

      var field = symbol as FSharpField;
      if (field != null)
      {
        // the field is in a property
        return new FSharpFieldProperty(this, field);
      }

      // the field is in a union case
      var unionCaseDecl = GetContainingNode<IUnionCaseDeclaration>();
      var containingUnionCase = unionCaseDecl?.GetFSharpSymbol() as FSharpUnionCase;
      if (containingUnionCase == null)
        return new FSharpFieldProperty(this, null);

      var caseFields = unionCaseDecl.Fields;
      var index = caseFields.IndexOf(this);
      Assertion.Assert(index != -1, "index != -1");

      var unionCaseFields = containingUnionCase.UnionCaseFields;
      var caseField = index <= unionCaseFields.Count
        ? unionCaseFields[index]
        : null;

      return new FSharpFieldProperty(this, caseField);
    }
  }
}