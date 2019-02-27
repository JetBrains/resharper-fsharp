using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpTypeParametersOwnerBase<TDeclaration> : FSharpFunctionBase<TDeclaration>
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    protected FSharpTypeParametersOwnerBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration, mfv)
    {
    }

    private IList<ITypeParameter> GetTypeParameters()
    {
      var mfvTypeParams = MfvParameters;
      if (mfvTypeParams.Count == 0)
        return EmptyList<ITypeParameter>.Instance;

      var outerTypeParamsCount = ContainingType?.GetAllTypeParameters().Count ?? 0;
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
        var mfvTypeParams = MfvParameters;
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

    private IList<FSharpGenericParameter> MfvParameters =>
      Mfv?.GenericParameters ?? EmptyList<FSharpGenericParameter>.Instance;
  }
}
