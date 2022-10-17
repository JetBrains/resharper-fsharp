using System;
using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

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

    public static IList<IList<string>> GetParametersGroupNames([NotNull] IFSharpParameterOwnerDeclaration decl) =>
      decl.ParameterGroups
        .Select(group => group.ParameterDeclarations.Select(paramDecl => paramDecl.ShortName).AsIList())
        .AsIList();

    public static DefaultValue GetParameterDefaultValue([NotNull] this IFSharpParameter param)
    {
      if (param.Symbol is not { } fcsParam)
        return null;

      try
      {
        // todo: implement DefaultValue in FCS
        var defaultValueAttr = fcsParam.Attributes.FirstOrDefault(a =>
            a.GetClrName() == PredefinedType.DEFAULTPARAMETERVALUE_ATTRIBUTE_CLASS.FullName)
          ?.ConstructorArguments.FirstOrDefault();

        var type = param.Type;
        return defaultValueAttr == null
          ? new DefaultValue(type, type)
          : new DefaultValue(type, ConstantValue.Create(defaultValueAttr.Item2, type));
      }
      // todo: change exception in FCS
      catch (Exception)
      {
        return DefaultValue.BAD_VALUE;
      }
    }
    
    public static ParameterKind GetParameterKind([CanBeNull] this FSharpParameter param)
    {
      if (param == null)
        return ParameterKind.VALUE;

      var fcsType = param.Type;
      if (fcsType.HasTypeDefinition && fcsType.TypeDefinition is var entity && entity.IsByRef)
      {
        if (param.Attributes.HasAttributeInstance(StandardTypeNames.OutAttribute) || entity.LogicalName == "outref`1")
          return ParameterKind.OUTPUT;
        if (param.IsInArg || entity.LogicalName == "inref`1")
          return ParameterKind.INPUT;

        return ParameterKind.REFERENCE;
      }

      return ParameterKind.VALUE;
    }

    public static bool IsParameterArray([NotNull] this IFSharpParameter fsParam) =>
      fsParam.ContainingParametersOwner is IFSharpFunction && 
      (fsParam.Symbol?.IsParamArrayArg ?? false);
  }
}
