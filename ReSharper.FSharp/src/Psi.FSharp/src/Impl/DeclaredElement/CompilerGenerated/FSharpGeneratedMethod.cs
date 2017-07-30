using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement.CompilerGenerated
{
  public class FSharpGeneratedMethod : FSharpGeneratedMemberBase, IMethod
  {
    public FSharpGeneratedMethod([NotNull] Class containingType,
      string shortName, IType paramType1, string paramName1, IType paramType2, string paramName2, IType returnType,
      bool isOverride = false, bool isStatic = false)
      : base(containingType)
    {
      ShortName = shortName;
      ReturnType = returnType;
      IsOverride = isOverride;
      IsStatic = isStatic;

      Parameters = new IParameter[]
      {
        new Parameter(this, 0, ParameterKind.VALUE, paramType1, paramName1),
        new Parameter(this, 1, ParameterKind.VALUE, paramType2, paramName2)
      };
    }

    public FSharpGeneratedMethod([NotNull] IClass containingType,
      string shortName, IType parameterType, string parameterName, IType returnType, bool isOverride = false,
      bool isStatic = false)
      : base(containingType)
    {
      ShortName = shortName;
      ReturnType = returnType;
      IsOverride = isOverride;
      IsStatic = isStatic;
      Parameters = new IParameter[] {new Parameter(this, 0, ParameterKind.VALUE, parameterType, parameterName)};
    }

    public FSharpGeneratedMethod([NotNull] Class containingType,
      string shortName, IType returnType, bool isOverride = false, bool isStatic = false)
      : base(containingType)
    {
      ShortName = shortName;
      ReturnType = returnType;
      IsOverride = isOverride;
      IsStatic = isStatic;
      Parameters = EmptyList<IParameter>.Instance;
    }

    public FSharpGeneratedMethod([NotNull] IClass containingType, string shortName, IReadOnlyList<IType> paramTypes,
      IReadOnlyList<string> paramNames, IType returnType, bool isOverride = false, bool isStatic = false) : base(
      containingType)
    {
      ShortName = shortName;
      ReturnType = returnType;
      IsOverride = isOverride;
      IsStatic = isStatic;

      var parameters = new LocalList<IParameter>();
      for (var i = 0; i < paramTypes.Count; i++)
        parameters.Add(new Parameter(this, i, ParameterKind.VALUE, paramTypes[i], paramNames[i]));

      Parameters = parameters.ResultingList();
    }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.METHOD;
    }

    public override string ShortName { get; }
    public override MemberHidePolicy HidePolicy => MemberHidePolicy.HIDE_BY_SIGNATURE;

    public InvocableSignature GetSignature(ISubstitution substitution)
    {
      return new InvocableSignature(this, substitution);
    }

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations()
    {
      return EmptyList<IParametersOwnerDeclaration>.Instance;
    }

    public IList<IParameter> Parameters { get; }
    public IType ReturnType { get; }
    public bool IsRefReturn => false;
    public bool IsPredefined => false;
    public bool IsIterator => false;
    public IAttributesSet ReturnTypeAttributes => EmptyAttributesSet.Instance;
    public bool IsExplicitImplementation => false;
    public IList<IExplicitImplementation> ExplicitImplementations => EmptyList<IExplicitImplementation>.Instance;
    public bool CanBeImplicitImplementation => true;
    public IList<ITypeParameter> TypeParameters => EmptyList<ITypeParameter>.Instance;
    public bool IsExtensionMethod => false;
    public bool IsAsync => false;
    public bool IsVarArg => false;
    public bool IsXamlImplicitMethod => false;
    public override bool IsSealed => true;
    public override bool IsOverride { get; }
    public override bool IsStatic { get; }
  }
}