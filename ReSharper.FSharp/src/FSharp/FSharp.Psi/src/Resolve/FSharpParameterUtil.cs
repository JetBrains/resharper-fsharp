using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public static class FSharpParameterUtil
  {
    [CanBeNull]
    public static IDeclaredElement GetOwner([NotNull] this FSharpParameter fsParameter,
      [NotNull] FSharpSymbolReference reference)
    {
      var referenceOwner = reference.GetElement();
      if (referenceOwner is IReferenceExpr referenceExpr)
      {
        var binaryAppExpr = BinaryAppExprNavigator.GetByLeftArgument(referenceExpr);
        if (binaryAppExpr is not { ShortName: "=" })
          return null;

        var innerExpr = (IFSharpExpression)TupleExprNavigator.GetByExpression(binaryAppExpr) ?? binaryAppExpr;
        var parenExpr = ParenOrBeginEndExprNavigator.GetByInnerExpression(innerExpr);

        if (!(PrefixAppExprNavigator.GetByArgumentExpression(parenExpr)?.FunctionExpression is IReferenceExpr expr))
          return null;

        var fcsSymbol = expr.Reference.GetFcsSymbol();
        switch (fcsSymbol)
        {
          case FSharpUnionCase unionCase:
            return GetFieldDeclaredElement(reference, unionCase, referenceOwner);

          case FSharpMemberOrFunctionOrValue mfv:
            // todo: fix member param declarations
            return mfv.GetDeclaredElement(referenceOwner.GetPsiModule(), referenceOwner) is IFunction functionElement
              ? functionElement.Parameters.FirstOrDefault(p => p.ShortName == reference.GetName())
              : null;
        }
      }

      if (referenceOwner is IExpressionReferenceName referenceName)
      {
        var fieldPat = FieldPatNavigator.GetByReferenceName(referenceName);
        var parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(fieldPat);
        if (parametersOwnerPat == null)
          return null;

        return parametersOwnerPat.ReferenceName.Reference.GetFcsSymbol() is FSharpUnionCase unionCase
          ? GetFieldDeclaredElement(reference, unionCase, referenceOwner)
          : null;
      }

      return null;
    }

    [CanBeNull]
    private static IDeclaredElement GetFieldDeclaredElement([NotNull] IReference reference,
      [NotNull] FSharpUnionCase unionCase, [NotNull] IFSharpReferenceOwner referenceOwner)
    {
      var field = unionCase.Fields.FirstOrDefault(f => f.Name == reference.GetName());
      return field?.GetDeclaredElement(referenceOwner.GetPsiModule(), referenceOwner);
    }
  }
}
