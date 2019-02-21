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

    private IList<ITypeParameter> GetTypeParameters(FSharpMemberOrFunctionOrValue mfv,
      ITypeMemberDeclaration declaration)
    {
      var mfvTypeParams = mfv.GenericParameters;
      if (mfvTypeParams.Count == 0)
        return EmptyList<ITypeParameter>.Instance;

      // todo: optional type extensions
      if (!(declaration.GetContainingTypeDeclaration() is IFSharpTypeDeclaration typeDeclaration))
        return EmptyList<ITypeParameter>.Instance;

      var outerTypeParamsCount = typeDeclaration.TypeParameters.Count;
      var typeParamsCount = mfvTypeParams.Count - outerTypeParamsCount;

      if (typeParamsCount == 0)
        return EmptyList<ITypeParameter>.Instance;

      var typeParams = new ITypeParameter[typeParamsCount];
      for (var i = 0; i < typeParamsCount; i++)
        typeParams[i] = new FSharpTypeParameterOfMethod(this, mfvTypeParams[i + outerTypeParamsCount].DisplayName, i);
      return typeParams;
    }

    public override IList<ITypeParameter> TypeParameters =>
      Mfv is var mfv && mfv != null && GetDeclaration() is var declaration && declaration != null
        ? GetTypeParameters(mfv, declaration)
        : EmptyList<ITypeParameter>.Instance;
  }
}
