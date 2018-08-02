using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.Util;
using static JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated.FSharpGeneratedMembers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public static class FSharpGeneratedMembers
  {
    public static readonly string[] SimpleTypeExtendsListShortNames =
      {"IStructuralEquatable", "IStructuralComparable", "IComparable"};

    public static readonly IClrTypeName StructuralComparableInterfaceName =
      new ClrTypeName("System.Collections.IStructuralComparable");

    public static readonly IClrTypeName StructuralEquatableInterfaceName =
      new ClrTypeName("System.Collections.IStructuralEquatable");

    public static readonly IClrTypeName ComparerInterfaceName =
      new ClrTypeName("System.Collections.IComparer");

    public static readonly IClrTypeName EqualityComparerInterfaceName =
      new ClrTypeName("System.Collections.IEqualityComparer");

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

    protected override IType SecondParamType => TypeFactory.CreateTypeByCLRName(ComparerInterfaceName, Module);
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

    protected override IType SecondParamType => TypeFactory.CreateTypeByCLRName(EqualityComparerInterfaceName, Module);
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

    protected override IType FirstParamType => TypeFactory.CreateTypeByCLRName(EqualityComparerInterfaceName, Module);
    protected override string FirstParamName => CompParameterName;
  }

  #endregion

  #region GetHashCodeMethod

  public class GetHashCodeMethod : FSharpGeneratedMethodBase
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

  public class ToStringMethod : FSharpGeneratedMethodBase
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

  public abstract class GeneratedMethodWithOneParam : FSharpGeneratedMethodBase
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

  #region IsUnionCaseProperty

  public class IsUnionCaseProperty : FSharpGeneratedPropertyBase
  {
    internal IsUnionCaseProperty([NotNull] ITypeElement typeElement, [NotNull] string shortName)
      : base(typeElement) => ShortName = shortName;

    public override string ShortName { get; }
    public override IType Type => PredefinedType.Bool;

    public new TypeElement TypeElement =>
      (TypeElement) base.TypeElement;

    public override AccessRights GetAccessRights() =>
      TypeElement.GetRepresentationAccessRights();
  }

  #endregion

  #region UnionTagProperty

  public class UnionTagProperty : FSharpGeneratedPropertyBase
  {
    public UnionTagProperty([NotNull] ITypeElement typeElement) : base(typeElement)
    {
    }

    public override string ShortName => "Tag";
    public override IType Type => PredefinedType.Int;

    public new TypeElement TypeElement =>
      (TypeElement) base.TypeElement;

    public override AccessRights GetAccessRights() =>
      TypeElement.GetRepresentationAccessRights();
  }

  #endregion

  #region NewUnionCaseMethod

  public class NewUnionCaseMethod : FSharpGeneratedMethodBase
  {
    internal readonly UnionCasePart UnionCasePart;

    public NewUnionCaseMethod([NotNull] TypeElement typeElement) : base(typeElement.GetContainingType()) =>
      UnionCasePart = typeElement.EnumerateParts().OfType<UnionCasePart>().First();

    public override string ShortName => "New" + UnionCasePart.ShortName;
    public override IType ReturnType => ContainingTypeType;

    public override bool IsStatic => true;

    public override IList<IParameter> Parameters
    {
      get
      {
        var fields = UnionCasePart.CaseFields;
        var result = new IParameter[fields.Count];
        for (var i = 0; i < fields.Count; i++)
        {
          var field = fields[i];
          result[i] = new Parameter(this, i, ParameterKind.VALUE, field.Type, field.ShortName.Decapitalize());
        }

        return result;
      }
    }

    public override AccessRights GetAccessRights() =>
      ContainingType.GetRepresentationAccessRights();
  }

  #endregion
}
