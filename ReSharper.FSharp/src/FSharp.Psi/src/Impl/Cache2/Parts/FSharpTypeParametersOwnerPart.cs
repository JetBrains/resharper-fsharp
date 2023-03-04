using System;
using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class FSharpTypeParametersOwnerPart<T> : FSharpTypePart<T> where T : class, IFSharpTypeOldDeclaration
  {
    private readonly string[] myTypeParameterNames;
    public override int MeasureTypeParametersCount { get; }

    protected FSharpTypeParametersOwnerPart([NotNull] T declaration, MemberDecoration memberDecoration,
      IList<ITypeParameterDeclaration> typeParameters, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder.Intern(declaration.CompiledName), memberDecoration, typeParameters.Count,
        cacheBuilder)
    {

      if (declaration is IFSharpTypeOrExtensionDeclaration { TypeParameterDeclarationList: { } typeParamDeclList })
        MeasureTypeParametersCount = typeParamDeclList.TypeParametersEnumerable.Count(typeParamDecl =>
          typeParamDecl.Attributes.HasAttribute("Measure"));

      if (typeParameters.Count == 0)
      {
        myTypeParameterNames = EmptyArray<string>.Instance;
        return;
      }

      myTypeParameterNames = new string[typeParameters.Count];
      for (var i = 0; i < typeParameters.Count; i++)
        myTypeParameterNames[i] = cacheBuilder.Intern(typeParameters[i].CompiledName);
    }

    protected FSharpTypeParametersOwnerPart(IReader reader) : base(reader)
    {
      var number = TypeParameterNumber;
      if (number == 0)
      {
        myTypeParameterNames = EmptyArray<string>.Instance;
        return;
      }

      myTypeParameterNames = new string[number];
      for (var index = 0; index < number; index++)
        myTypeParameterNames[index] = reader.ReadString();
    }

    protected override void AssignDeclaredElement(ICachedDeclaration2 cachedDeclaration)
    {
      base.AssignDeclaredElement(cachedDeclaration);

      var parameters = TypeElement?.TypeParameters;
      if (parameters == null || parameters.IsEmpty())
        return;

      if (!(cachedDeclaration is IFSharpTypeOrExtensionDeclaration declaration)) return;

      var typeParameterDeclarations = declaration.TypeParameterDeclarations;
      var parametersCount = Math.Min(parameters.Count, typeParameterDeclarations.Count);

      for (var i = 0; i < parametersCount; i++)
        AssignToCachedDeclaration((ICachedDeclaration2) typeParameterDeclarations[i], parameters[i]);
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      foreach (var parameterName in myTypeParameterNames)
        writer.WriteString(parameterName);
    }

    private ITypeParameterOfTypeDeclaration GetTypeParameterDeclaration(IFSharpTypeOrExtensionDeclaration typeDecl,
      int index) =>
      typeDecl != null && index < TypeParameterNumber
        ? typeDecl.TypeParameterDeclarations[index] as ITypeParameterOfTypeDeclaration
        : null;

    public override IDeclaration GetTypeParameterDeclaration(int index) =>
      GetTypeParameterDeclaration(GetDeclaration() as IFSharpTypeOrExtensionDeclaration, index);

    public override string GetTypeParameterName(int index) =>
      myTypeParameterNames[index];

    public override TypeParameterVariance GetTypeParameterVariance(int index) =>
      TypeParameterVariance.INVARIANT;

    private IEnumerable<ITypeConstraint> GetAllTypeParameterConstraints()
    {
      if (!(GetDeclaration() is IFSharpTypeDeclaration typeDecl))
        yield break;

      if (typeDecl.PostfixTypeParameterList is {TypeConstraintsClause: { } typeParamListClause})
        foreach (var constraint in typeParamListClause.Constraints)
          yield return constraint;

      if (typeDecl.TypeConstraintsClause is { } typeDeclClause)
        foreach (var constraint in typeDeclClause.Constraints)
          yield return constraint;
    }

    private IEnumerable<ITypeConstraint> GetTypeParameterConstraints(string name)
    {
      if (name == SharedImplUtil.MISSING_DECLARATION_NAME)
        yield break;

      foreach (var typeConstraint in GetAllTypeParameterConstraints())
      foreach (var referenceName in typeConstraint.ReferenceNames)
      {
        if (referenceName.FSharpIdentifier.GetSourceName() == name)
          yield return typeConstraint;
      }
    }

    private IEnumerable<ITypeConstraint> GetTypeParameterConstraints(int index) =>
      GetTypeParameterConstraints(GetTypeParameterName(index));

    public override IEnumerable<IType> GetTypeParameterSuperTypes(int index)
    {
      if (!(GetDeclaration() is IFSharpTypeDeclaration typeDecl)) 
        yield break;

      var constraints = GetTypeParameterConstraints(index);
      if (!constraints.Any(constraint => constraint is ISubtypeConstraint))
        yield break;

      if (!(typeDecl.GetFcsSymbol() is FSharpEntity fcsEntity))
        yield break;

      var fcsTypeParameters = fcsEntity.GenericParameters;
      if (fcsTypeParameters.Count <= index)
        yield break;

      var fcsTypeParameter = fcsTypeParameters[index];
      foreach (var fcsConstraint in fcsTypeParameter.Constraints)
        if (fcsConstraint.IsCoercesToConstraint)
          yield return fcsConstraint.CoercesToTarget.MapType(typeDecl);
    }

    public override TypeParameterConstraintFlags GetTypeParameterConstraintFlags(int index) =>
      GetTypeParameterName(index) is var name && name != SharedImplUtil.MISSING_DECLARATION_NAME
        ? GetTypeConstraintFlags(name, GetTypeParameterConstraints(name))
        : TypeParameterConstraintFlags.None;

    public override bool IsNullableContextEnabledForTypeParameter(int index) => false;

    private static TypeParameterConstraintFlags GetTypeConstraintFlags(string typeParamName,
      IEnumerable<ITypeConstraint> constraints)
    {
      var flags = TypeParameterConstraintFlags.None;
      foreach (var typeConstraint in constraints)
      {
        foreach (var referenceName in typeConstraint.ReferenceNames)
        {
          if (referenceName.FSharpIdentifier.GetSourceName() == typeParamName)
            flags |= GetTypeConstraintFlags(typeParamName, typeConstraint);
        }
      }

      return flags;
    }

    private static TypeParameterConstraintFlags GetTypeConstraintFlags(string paramName, ITypeConstraint constraint)
    {
      bool IsSimpleNamedTypeUsage(ITypeUsage typeUsage, string name) =>
        typeUsage.IgnoreInnerParens() is INamedTypeUsage {ReferenceName: {Qualifier: null} returnName} &&
        returnName.ShortName == name;

      var flags = TypeParameterConstraintFlags.None;
      switch (constraint)
      {
        case IValueTypeConstraint _:
          flags |= TypeParameterConstraintFlags.ValueType;
          break;

        case INullConstraint _:
        case IReferenceTypeConstraint _:
          flags |= TypeParameterConstraintFlags.ReferenceType;
          break;

        case IUnmanagedTypeConstraint _:
          flags |= TypeParameterConstraintFlags.Unmanaged;
          break;

        case IMemberConstraint memberConstraint:
          if (!(memberConstraint.MemberSignature is IConstructorSignature ctorSig))
            break;

          if (!(ctorSig.ReturnTypeInfo?.ReturnType.IgnoreParameterSignature() is IFunctionTypeUsage typeUsage))
            break;

          if (IsSimpleNamedTypeUsage(typeUsage.ReturnTypeUsage, paramName) &&
              IsSimpleNamedTypeUsage(typeUsage.ArgumentTypeUsage, "unit"))
            flags |= TypeParameterConstraintFlags.Constructor;
          break;

        case ISubtypeConstraint _:
        case IDefaultsToConstraint _:
        case IComparableConstraint _:
        case IEquatableConstraint _:
        case IDelegateConstraint _:
        case IEnumConstraint _:
          break;

        default:
          throw new ArgumentOutOfRangeException(nameof(constraint));
      }

      return flags;
    }

    protected override string PrintTypeParameters() =>
      myTypeParameterNames.Length == 0
        ? ""
        : "<" + StringUtil.StringArrayText(myTypeParameterNames) + ">";
  }
}
