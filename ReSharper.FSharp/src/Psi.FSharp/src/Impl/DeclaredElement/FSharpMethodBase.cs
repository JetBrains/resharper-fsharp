using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpOperator<TDeclaration> : FSharpOperatorBase<TDeclaration>
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    public FSharpOperator([NotNull] ITypeMemberDeclaration declaration, [NotNull] FSharpMemberOrFunctionOrValue mfv,
      [CanBeNull] IFSharpTypeDeclaration typeDeclaration) : base(declaration, mfv, typeDeclaration)
    {
      var returnType =
        FSharpTypesUtil.GetType(mfv.ReturnParameter.Type, declaration, EmptyList<ITypeParameter>.Instance, Module) ??
        TypeFactory.CreateUnknownType(Module);
      ReturnType = returnType.IsUnit(Module) ? Module.GetPredefinedType().Void : returnType;
    }

    public override IType ReturnType { get; }
  }

  internal abstract class FSharpOperatorBase<TDeclaration> : FSharpFunctionBase<TDeclaration>, ISignOperator
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    protected FSharpOperatorBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv, [CanBeNull] IFSharpTypeDeclaration typeDeclaration)
      : base(declaration, mfv, typeDeclaration)
    {
    }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.SIGN_OPERATOR;
    }

//    public override string ShortName => CommonUtil.DecompileOpName(FSharpSymbol.CompiledName);
    public override string ShortName => FSharpSymbol.CompiledName;
    public override bool IsStatic => true;
  }

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

      var returnType = FSharpTypesUtil.GetType(mfv.ReturnParameter.Type, declaration, TypeParameters, Module) ??
                       TypeFactory.CreateUnknownType(Module);
      ReturnType = returnType.IsUnit(Module) ? Module.GetPredefinedType().Void : returnType;


      var methodParams = new FrugalLocalList<IParameter>();
      foreach (var paramsGroup in mfv.CurriedParameterGroups)
      foreach (var param in paramsGroup)
      {
        var paramType = param.Type;
        methodParams.Add(new FSharpMethodParameter(param, this, methodParams.Count,
          FSharpTypesUtil.GetParameterKind(param),
          FSharpTypesUtil.GetType(paramType, declaration, TypeParameters, Module), param.DisplayName));
      }
      Parameters = methodParams.Count == 1 && methodParams[0].Type.IsUnit(Module)
        ? EmptyList<IParameter>.InstanceList
        : methodParams.ToList();
    }

    public override IList<IParameter> Parameters { get; }
    public IList<ITypeParameter> TypeParameters { get; }
    public override IType ReturnType { get; }

    public bool IsPredefined => false;
    public bool IsIterator => false;
    public IAttributesSet ReturnTypeAttributes => EmptyAttributesSet.Instance;
  }

  internal abstract class FSharpMethodBase<TDeclaration> : FSharpFunctionBase<TDeclaration>, IMethod
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    protected FSharpMethodBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv, [CanBeNull] IFSharpTypeDeclaration typeDeclaration)
      : base(declaration, mfv, typeDeclaration)
    {
      ShortName = mfv.GetMemberCompiledName();
    }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.METHOD;
    }

    public override string ShortName { get; }
    public bool IsExtensionMethod => false;
    public bool IsAsync => false;
    public bool IsVarArg => false;
    public bool IsXamlImplicitMethod => false;
  }
}