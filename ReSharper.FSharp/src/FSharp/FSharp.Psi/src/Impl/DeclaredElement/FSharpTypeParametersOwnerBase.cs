using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpTypeParametersOwnerBase<TDeclaration> : FSharpFunctionBase<TDeclaration>
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    protected FSharpTypeParametersOwnerBase([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    private IList<ITypeParameter> GetTypeParameters()
    {
      var mfvTypeParams = MfvTypeParameters;
      if (mfvTypeParams.Count == 0)
        return EmptyList<ITypeParameter>.Instance;

      var outerTypeParamsCount = GetContainingType()?.GetAllTypeParameters().Count ?? 0;
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
        if (mfvParametersCount == 0)
          return EmptyList<ITypeParameter>.Instance;

        var outerTypeParameters = base.AllTypeParameters;
        var outerTypeParametersCount = outerTypeParameters.Count;

        var typeParams = new ITypeParameter[mfvParametersCount];
        for (var i = 0; i < outerTypeParametersCount; i++)
          typeParams[i] = outerTypeParameters[i];

        for (var i = outerTypeParametersCount; i < mfvParametersCount; i++)
          typeParams[i] = new FSharpTypeParameterOfMethod(this, mfvTypeParams[i].Name, i - outerTypeParametersCount);
        return typeParams;
      }
    }

    public override IList<ITypeParameter> TypeParameters => GetTypeParameters();

    protected virtual IList<FSharpGenericParameter> MfvTypeParameters =>
      Mfv?.GenericParameters.Where(fcsTypeParameter => !fcsTypeParameter.IsMeasure).ToIList() ??
      EmptyList<FSharpGenericParameter>.Instance;
  }
}
