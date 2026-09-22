using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpTypeParametersOwnerBase<TDeclaration>([NotNull] ITypeMemberDeclaration declaration)
    : FSharpFunctionBase<TDeclaration>(declaration)
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    private IList<ITypeParameter> GetTypeParameters()
    {
      var mfvTypeParams = MfvTypeParameters;
      if (mfvTypeParams.Count == 0)
        return EmptyList<ITypeParameter>.Instance;

      var outerTypeParamsCount = GetContainingType() is { } containingType and not FSharpObjectExpressionClass
        ? containingType.GetAllTypeParameters().Count
        : 0;
      var typeParamsCount = mfvTypeParams.Count - outerTypeParamsCount;

      if (typeParamsCount == 0)
        return EmptyList<ITypeParameter>.Instance;

      var typeParams = new ITypeParameter[typeParamsCount];
      for (var i = 0; i < typeParamsCount; i++)
        typeParams[i] = new FSharpTypeParameterOfMethod(this, mfvTypeParams[i + outerTypeParamsCount].Name, i);
      return typeParams;
    }

    public override IList<ITypeParameter> AllTypeParameters
    {
      get
      {
        var mfvTypeParams = MfvTypeParameters;
        var mfvParametersCount = mfvTypeParams.Count;
        var isObjExprMember = GetContainingType() is FSharpObjectExpressionClass;
        if (mfvParametersCount == 0 && !isObjExprMember)
          return EmptyList<ITypeParameter>.Instance;

        var outerTypeParameters = base.AllTypeParameters;
        var outerTypeParametersCount = outerTypeParameters.Count;
        var mfvOuterTypeParametersCount = isObjExprMember ? 0 : outerTypeParametersCount;

        var typeParams = new ITypeParameter[outerTypeParametersCount + mfvParametersCount - mfvOuterTypeParametersCount];
        for (var i = 0; i < outerTypeParametersCount; i++)
          typeParams[i] = outerTypeParameters[i];

        for (var i = mfvOuterTypeParametersCount; i < mfvParametersCount; i++)
          typeParams[outerTypeParametersCount + i - mfvOuterTypeParametersCount] =
            new FSharpTypeParameterOfMethod(this, mfvTypeParams[i].Name, i - mfvOuterTypeParametersCount);
        return typeParams;
      }
    }

    public override IList<ITypeParameter> TypeParameters => GetTypeParameters();
    public int TypeParametersCount => GetTypeParameters().Count; // todo: optimize?

    protected virtual IList<FSharpGenericParameter> MfvTypeParameters =>
      Mfv?.GenericParameters.Where(fcsTypeParameter => !fcsTypeParameter.IsMeasure).ToIList() ??
      EmptyList<FSharpGenericParameter>.Instance;
  }
}
