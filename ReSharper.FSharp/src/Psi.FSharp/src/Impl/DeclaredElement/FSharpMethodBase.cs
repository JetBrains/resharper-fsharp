using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  internal abstract class FSharpMethodBase<TDeclaration> : FSharpMemberBase<TDeclaration>, IMethod
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    protected FSharpMethodBase([NotNull] ITypeMemberDeclaration declaration,
      [CanBeNull] FSharpMemberOrFunctionOrValue mfv,
      [CanBeNull] IFSharpTypeParametersOwnerDeclaration typeDeclaration) : base(declaration, mfv)
    {
      if (mfv == null)
      {
        TypeParameters = EmptyList<ITypeParameter>.Instance;
        ReturnType = TypeFactory.CreateUnknownType(Module);
        Parameters = EmptyList<IParameter>.Instance;
        ShortName = declaration.DeclaredName;
        return;
      }

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
        var isParamArray = FSharpTypesUtil.IsParamArray(param);
        methodParams.Add(new FSharpParameter(this, methodParams.Count, FSharpTypesUtil.GetParameterKind(param),
          FSharpTypesUtil.GetType(paramType, declaration, TypeParameters, Module), param.DisplayName, isParamArray));
      }
      Parameters = methodParams.Count == 1 && methodParams[0].Type.IsUnit(Module)
        ? EmptyList<IParameter>.InstanceList
        : methodParams.ToList();

      ShortName = mfv.GetMemberCompiledName();
    }

    public override string ShortName { get; }
    public override IType ReturnType { get; }
    public override IList<IParameter> Parameters { get; }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.METHOD;
    }

    public bool IsPredefined => false;
    public bool IsIterator => false;
    public IAttributesSet ReturnTypeAttributes => EmptyAttributesSet.Instance;
    public bool IsExplicitImplementation => false;
    public IList<IExplicitImplementation> ExplicitImplementations => EmptyList<IExplicitImplementation>.Instance;
    public IList<ITypeParameter> TypeParameters { get; }
    public bool IsExtensionMethod => false;
    public bool IsAsync => false;
    public bool IsVarArg => false;
    public bool IsXamlImplicitMethod => false;
  }
}