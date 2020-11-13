using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl.Special;
using static JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated.FSharpGeneratedMembers;
using static JetBrains.ReSharper.Plugins.FSharp.Util.FSharpPredefinedType;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public static class FSharpGeneratedMembers
  {
    public static readonly string[] SimpleTypeExtendsListShortNames =
      {"IStructuralEquatable", "IStructuralComparable", "IComparable"};

    public const string CompareToMethodName = "CompareTo";
    public const string EqualsMethodName = "Equals";
    public const string GetHashCodeMethodName = "GetHashCode";

    public const string ObjParameterName = "obj";
    public const string CompParameterName = "comp";
    public const string InfoParameterName = "info";
    public const string ContextParameterName = "context";
  }

  #region CompareToObjectWithComparerMethod

  public class CompareToObjectWithComparerMethod : GeneratedMethodWithTwoParams
  {
    public CompareToObjectWithComparerMethod([NotNull] ITypeElement containingType) : base(containingType)
    {
    }

    public override string ShortName => CompareToMethodName;
    public override IType ReturnType => PredefinedType.Int;

    protected override IType FirstParamType => PredefinedType.Object;
    protected override string FirstParamName => ObjParameterName;

    protected override IType SecondParamType => ComparerTypeName.CreateTypeByClrName(Module);
    protected override string SecondParamName => CompParameterName;
  }

  #endregion

  #region CompareToObjectMethod

  public class CompareToObjectMethod : GeneratedMethodWithOneParam
  {
    public CompareToObjectMethod([NotNull] ITypeElement containingMember) : base(containingMember)
    {
    }

    public override string ShortName => CompareToMethodName;
    public override IType ReturnType => PredefinedType.Int;

    protected override IType FirstParamType => PredefinedType.Object;
    protected override string FirstParamName => ObjParameterName;
  }

  #endregion

  #region CompareToSimpleTypeMethod

  public class CompareToSimpleTypeMethod : GeneratedMethodWithOneParam
  {
    public CompareToSimpleTypeMethod([NotNull] ITypeElement containingType) : base(containingType)
    {
    }

    public override string ShortName => CompareToMethodName;
    public override IType ReturnType => PredefinedType.Int;

    protected override IType FirstParamType => ContainingTypeType;
    protected override string FirstParamName => ObjParameterName;
  }

  #endregion

  #region EqualsObjectWithComparerMethod

  public class EqualsObjectWithComparerMethod : GeneratedMethodWithTwoParams
  {
    public EqualsObjectWithComparerMethod([NotNull] ITypeElement containingType) : base(containingType)
    {
    }

    public override string ShortName => EqualsMethodName;
    public override IType ReturnType => PredefinedType.Bool;

    protected override IType FirstParamType => PredefinedType.Object;
    protected override string FirstParamName => ObjParameterName;

    protected override IType SecondParamType => EqualityComparerTypeName.CreateTypeByClrName(Module);
    protected override string SecondParamName => CompParameterName;
  }

  #endregion

  #region EqualsObjectMethod

  public class EqualsObjectMethod : EqualsSimpleTypeMethod
  {
    public EqualsObjectMethod([NotNull] ITypeElement containingType) : base(containingType)
    {
    }

    protected override IType FirstParamType => PredefinedType.Object;
    public override bool IsOverride => true;
  }

  #endregion

  #region EqualsSimpleTypeMethod

  public class EqualsSimpleTypeMethod : GeneratedMethodWithOneParam
  {
    public EqualsSimpleTypeMethod([NotNull] ITypeElement containingType) : base(containingType)
    {
    }

    public override string ShortName => EqualsMethodName;
    public override IType ReturnType => PredefinedType.Bool;

    protected override IType FirstParamType => ContainingTypeType;
    protected override string FirstParamName => ObjParameterName;
  }

  #endregion

  #region GetHashCodeWithComparerMethod

  public class GetHashCodeWithComparerMethod : GeneratedMethodWithOneParam
  {
    public GetHashCodeWithComparerMethod([NotNull] ITypeElement containingType) : base(containingType)
    {
    }

    public override string ShortName => GetHashCodeMethodName;
    public override IType ReturnType => PredefinedType.Int;

    protected override IType FirstParamType => EqualityComparerTypeName.CreateTypeByClrName(Module);
    protected override string FirstParamName => CompParameterName;
  }

  #endregion

  #region GetHashCodeMethod

  public class GetHashCodeMethod : FSharpGeneratedMethodFromTypeBase
  {
    public GetHashCodeMethod([NotNull] ITypeElement containingType) : base(containingType)
    {
    }

    public override string ShortName => GetHashCodeMethodName;
    public override IType ReturnType => PredefinedType.Int;

    public override bool IsOverride => true;
  }

  #endregion

  #region ToStringMethod

  public class ToStringMethod : FSharpGeneratedMethodFromTypeBase
  {
    public ToStringMethod([NotNull] ITypeElement containingType) : base(containingType)
    {
    }

    public override string ShortName => StandardMemberNames.ObjectToString;
    public override IType ReturnType => PredefinedType.String;

    public override bool IsOverride => true;
  }

  #endregion

  #region GeneratedMethodWithTwoParams

  public abstract class GeneratedMethodWithTwoParams : GeneratedMethodWithOneParam
  {
    protected GeneratedMethodWithTwoParams([NotNull] ITypeElement containingType) : base(containingType)
    {
    }

    protected abstract IType SecondParamType { get; }
    protected abstract string SecondParamName { get; }

    public override IList<IParameter> Parameters => new IParameter[]
    {
      new Parameter(this, 0, ParameterKind.VALUE, FirstParamType, FirstParamName),
      new Parameter(this, 1, ParameterKind.VALUE, SecondParamType, SecondParamName)
    };
  }

  #endregion

  #region GeneratedMethodWithOneParam

  public abstract class GeneratedMethodWithOneParam : FSharpGeneratedMethodFromTypeBase
  {
    protected GeneratedMethodWithOneParam([NotNull] ITypeElement containingType) : base(containingType)
    {
    }

    protected abstract IType FirstParamType { get; }
    protected abstract string FirstParamName { get; }

    public override IList<IParameter> Parameters =>
      new IParameter[] {new Parameter(this, 0, ParameterKind.VALUE, FirstParamType, FirstParamName)};
  }

  #endregion

  #region ExceptionConstructor

  public class ExceptionConstructor : FSharpGeneratedConstructor
  {
    public ExceptionConstructor(TypePart typePart) : base(typePart)
    {
    }

    public override IList<IParameter> Parameters =>
      new IParameter[]
      {
        new Parameter(this, 0, ParameterKind.VALUE, PredefinedType.SerializationInfo, InfoParameterName),
        new Parameter(this, 1, ParameterKind.VALUE, PredefinedType.StreamingContext, ContextParameterName)
      };

    public override AccessRights GetAccessRights() => AccessRights.PROTECTED;
  }

  #endregion

  #region UnionTagProperty

  public class UnionTagProperty : FSharpGeneratedPropertyFromTypeBase
  {
    public UnionTagProperty([NotNull] ITypeElement typeElement) : base(typeElement)
    {
    }

    public override string ShortName => "Tag";
    public override IType Type => PredefinedType.Int;

    public override AccessRights GetAccessRights() =>
      ContainingType.GetRepresentationAccessRights();
  }

  #endregion
}
