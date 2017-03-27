using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class FSharpFieldDeclaration
  {
    public override string DeclaredName => Identifier.GetName();

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var field = GetFSharpSymbol() as FSharpField;
      if (field != null)
      {
        // the field is in a property
        return new FSharpFieldProperty(this, field);
      }

      // the field is in a union case
      var unionCaseDecl = GetContainingNode<IFSharpUnionCaseDeclaration>();
      var unionCase = unionCaseDecl?.GetFSharpSymbol() as FSharpUnionCase;
      if (unionCase == null)
        return new FSharpFieldProperty(this, null);

      var caseFields = unionCaseDecl.Fields;
      var index = caseFields.IndexOf(this);
      Assertion.Assert(index != -1, "index != -1");

      var unionCaseFields = unionCase.UnionCaseFields;
      var caseField = index <= unionCaseFields.Count
        ? unionCaseFields[index]
        : null;

      return new FSharpFieldProperty(this, caseField);
    }
  }
}