using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  /// <summary>
  /// Map FSharpType elements (as seen by FSharp.Compiler.Service) to IType types.
  /// </summary>
  public class FSharpTypesUtil
  {
    [CanBeNull]
    public static IDeclaredType GetBaseType([NotNull] FSharpEntity entity,
      IList<ITypeParameter> typeParametersFromContext, [NotNull] IPsiModule psiModule)
    {
      return entity.BaseType != null
        ? GetType(entity.BaseType.Value, typeParametersFromContext, psiModule) as IDeclaredType
        : TypeFactory.CreateUnknownType(psiModule);
    }

    [NotNull]
    public static IEnumerable<IDeclaredType> GetSuperTypes([NotNull] FSharpEntity entity,
      IList<ITypeParameter> typeParametersFromContext, [NotNull] IPsiModule psiModule)
    {
      var interfaces = entity.DeclaredInterfaces;
      var types = new List<IDeclaredType>(interfaces.Count + 1);
      foreach (var entityInterface in interfaces)
      {
        var declaredType = GetType(entityInterface, typeParametersFromContext, psiModule) as IDeclaredType;
        if (declaredType != null) types.Add(declaredType);
      }
      var baseType = GetBaseType(entity, typeParametersFromContext, psiModule);
      if (baseType != null) types.Add(baseType);
      return types;
    }

    [NotNull]
    public static string GetClrName([NotNull] FSharpEntity entity)
    {
      // sometimes name includes assembly name, public key, etc and separated with comma
      return entity.QualifiedName.SubstringBefore(",");
    }

    [CanBeNull]
    private static string GetClrName([NotNull] FSharpType fsType)
    {
      var typeArgumentsCount = fsType.GenericArguments.Count;

      if (fsType.IsTupleType)
        return "System.Tuple`" + typeArgumentsCount;

      if (fsType.IsFunctionType)
        return "Microsoft.FSharp.Core.FSharpFunc`" + typeArgumentsCount;

      return fsType.TypeDefinition.QualifiedName.SubstringBefore(",");
    }

    /// <summary>
    /// Get type from a context of some declaration, possibly containing type parameters declarations.
    /// </summary>
    [CanBeNull]
    public static IType GetType([NotNull] FSharpType fsType,
      [NotNull] ITypeMemberDeclaration typeMemberDeclaration, [NotNull] IPsiModule psiModule)
    {
      return GetType(fsType, GetTypeParametersFromOuterType(typeMemberDeclaration), psiModule);
    }

    [CanBeNull]
    public static IType GetType([NotNull] FSharpType fsType,
      [NotNull] ITypeMemberDeclaration methodDeclaration, [NotNull] IList<ITypeParameter> methodTypeParams,
      [NotNull] IPsiModule psiModule)
    {
      var typeParametersFromType = GetTypeParametersFromOuterType(methodDeclaration);
      var typeParametersFromContext = typeParametersFromType?.Prepend(methodTypeParams).ToIList() ?? methodTypeParams;
      return GetType(fsType, typeParametersFromContext, psiModule);
    }

    [CanBeNull]
    private static IList<ITypeParameter> GetTypeParametersFromOuterType(ITypeMemberDeclaration typeMemberDeclaration)
    {
      var typeDeclaration = typeMemberDeclaration.GetContainingTypeDeclaration();
      return typeDeclaration?.DeclaredElement?.TypeParameters;
    }

    [CanBeNull]
    public static IType GetType([NotNull] FSharpType fsType,
      [CanBeNull] IList<ITypeParameter> typeParametersFromContext, [NotNull] IPsiModule psiModule)
    {
      var type = GetAbbreviatedType(fsType);

      if (type.IsGenericParameter)
        return FindTypeParameterByName(type, typeParametersFromContext, psiModule);

      if (type.HasTypeDefinition && type.TypeDefinition.IsArrayType)
        return GetArrayType(type, typeParametersFromContext, psiModule);

      var qualifiedName = GetClrName(type);
      if (qualifiedName == null)
        return TypeFactory.CreateUnknownType(psiModule);

      var declaredType = TypeFactory.CreateTypeByCLRName(qualifiedName, psiModule);
      var typeElement = declaredType.GetTypeElement();
      if (typeElement == null)
        return TypeFactory.CreateUnknownType(psiModule);

      var args = type.GenericArguments;
      return args.Count != 0
        ? GetTypeWithSubstitution(typeElement, args, typeParametersFromContext, psiModule) ?? declaredType
        : declaredType;
    }

    [CanBeNull]
    private static IType GetArrayType([NotNull] FSharpType fsType,
      [CanBeNull] IList<ITypeParameter> typeParametersFromContext, [NotNull] IPsiModule psiModule)
    {
      var entity = fsType.TypeDefinition;
      Assertion.Assert(entity.IsArrayType, "fsType.TypeDefinition.IsArrayType");
      Assertion.Assert(fsType.GenericArguments.Count == 1, "fsType.GenericArguments.Count == 1");

      var arrayType = GetTypeArgumentType(fsType.GenericArguments[0], null, typeParametersFromContext, psiModule);
      return TypeFactory.CreateArrayType(arrayType ?? TypeFactory.CreateUnknownType(psiModule), entity.ArrayRank);
    }

    [CanBeNull]
    private static IDeclaredType GetTypeWithSubstitution([NotNull] ITypeElement typeElement, IList<FSharpType> fsTypes,
      [CanBeNull] IList<ITypeParameter> typeParametersFromContext, [NotNull] IPsiModule psiModule)
    {
      var typeParams = typeElement.TypeParameters;
      var typeArgs = new IType[fsTypes.Count];
      for (var i = 0; i < fsTypes.Count; i++)
      {
        var typeParam = typeParams.Count >= i + 1 ? typeParams[i] : null;
        var typeArg = GetTypeArgumentType(fsTypes[i], typeParam?.Type(), typeParametersFromContext, psiModule);
        if (typeArg == null)
          return TypeFactory.CreateUnknownType(psiModule);
        typeArgs[i] = typeArg;
      }

      return TypeFactory.CreateType(typeElement, typeArgs);
    }

    [CanBeNull]
    private static IType GetTypeArgumentType([NotNull] FSharpType arg, [CanBeNull] IType typeParam,
      [CanBeNull] IList<ITypeParameter> typeParametersFromContext, [NotNull] IPsiModule psiModule)
    {
      return arg.IsGenericParameter
        ? FindTypeParameterByName(arg, typeParametersFromContext, psiModule) ?? typeParam
        : GetType(arg, typeParametersFromContext, psiModule);
    }

    [CanBeNull]
    private static IType FindTypeParameterByName([NotNull] FSharpType type,
      [CanBeNull] IEnumerable<ITypeParameter> typeParameters, [NotNull] IPsiModule psiModule)
    {
      Assertion.Assert(type.IsGenericParameter, "type.IsGenericParameter");
      var typeParam = typeParameters?.FirstOrDefault(
        typeParameter => typeParameter.ShortName == type.GenericParameter.DisplayName);

      return typeParam != null
        ? TypeFactory.CreateType(typeParam)
        : TypeFactory.CreateUnknownType(psiModule);
      ;
    }

    [NotNull]
    private static FSharpType GetAbbreviatedType([NotNull] FSharpType fsType)
    {
      while (fsType.IsAbbreviation)
        fsType = fsType.AbbreviatedType;

      return fsType;
    }
  }
}