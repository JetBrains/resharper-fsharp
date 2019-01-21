using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpFunctionBase<TDeclaration> : FSharpMemberBase<TDeclaration>, IFunction
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    protected FSharpFunctionBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv, [CanBeNull] IFSharpTypeDeclaration typeDeclaration)
      : base(declaration, mfv)
    {
      var mfvTypeParams = mfv.GenericParameters;
      var typeParams = new FrugalLocalList<ITypeParameter>();
      var outerTypeParamsCount = typeDeclaration?.TypeParameters.Count ?? 0;
      for (var i = outerTypeParamsCount; i < mfvTypeParams.Count; i++)
        typeParams.Add(new FSharpTypeParameterOfMethod(this, mfvTypeParams[i].DisplayName, i - outerTypeParamsCount));
      TypeParameters = typeParams.ToList();

      ReturnType = mfv.IsConstructor || mfv.ReturnParameter.Type.IsUnit
        ? Module.GetPredefinedType().Void
        : FSharpTypesUtil.GetType(mfv.ReturnParameter.Type, declaration, TypeParameters, Module, true) ??
          TypeFactory.CreateUnknownType(Module);

      var methodParams = new FrugalLocalList<IParameter>();
      var mfvParamGroups = mfv.CurriedParameterGroups;
      if (mfvParamGroups.Count == 1 && mfvParamGroups[0].Count == 1 && mfvParamGroups[0][0].Type.IsUnit)
      {
        Parameters = EmptyList<IParameter>.InstanceList;
        return;
      }

      foreach (var paramsGroup in mfv.CurriedParameterGroups)
      foreach (var param in paramsGroup)
      {
        var paramType = param.Type;
        var paramName = param.DisplayName;
        methodParams.Add(new FSharpMethodParameter(param, this, methodParams.Count,
          FSharpTypesUtil.GetParameterKind(param),
          FSharpTypesUtil.GetType(paramType, declaration, TypeParameters, Module, false),
          paramName.IsEmpty() ? SharedImplUtil.MISSING_DECLARATION_NAME : paramName));
      }
      Parameters = methodParams.ToList();
    }

    public override bool Equals(object obj)
    {
      if (!base.Equals(obj))
        return false;

      if (!(obj is FSharpMemberBase<TDeclaration> member) || IsStatic != member.IsStatic) // RIDER-11321, RSRP-467025
        return false;

      return SignatureComparers.Strict.CompareWithoutName(GetSignature(IdSubstitution),
        member.GetSignature(member.IdSubstitution));
    }

    public override int GetHashCode() => ShortName.GetHashCode();
    
    public override IList<IParameter> Parameters { get; }
    public IList<ITypeParameter> TypeParameters { get; }
    public override IType ReturnType { get; }

    public bool IsPredefined => false;
    public bool IsIterator => false;
    public IAttributesSet ReturnTypeAttributes => new FSharpAttributeSet(FSharpSymbol.Attributes, Module);
  }
}