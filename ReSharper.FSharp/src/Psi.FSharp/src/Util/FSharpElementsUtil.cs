using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  /// <summary>
  /// Map FSharpSymbol elements (as seen by FSharp.Compiler.Service) to declared elements or types.
  /// </summary>
  public class FSharpElementsUtil
  {
    [CanBeNull]
    public static IDeclaredType GetBaseType([NotNull] FSharpEntity entity,
      IList<ITypeParameter> typeParametersFromContext, [NotNull] IPsiModule psiModule)
    {
      return entity.BaseType != null
        ? GetType(entity.BaseType.Value, typeParametersFromContext, psiModule) as IDeclaredType
        : null;
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
    private static string GetQualifiedName([NotNull] FSharpEntity entity)
    {
      // sometimes name includes assembly name, public key, etc and separated with comma
      return entity.QualifiedName.SubstringBefore(",");
    }

    [CanBeNull]
    private static string GetQualifiedName([NotNull] FSharpType fsType)
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
      var typeDeclaration = typeMemberDeclaration.GetContainingTypeDeclaration();
      var typeParameters = typeDeclaration?.DeclaredElement?.TypeParameters; // todo obj type member type params
      return GetType(fsType, typeParameters, psiModule);
    }

    [CanBeNull]
    private static IType GetType([NotNull] FSharpType fsType,
      [CanBeNull] IList<ITypeParameter> typeParametersFromContext, [NotNull] IPsiModule psiModule)
    {
      var type = GetAbbreviatedType(fsType);

      if (type.IsGenericParameter)
        return FindTypeParameterByName(type, typeParametersFromContext);

      if (type.HasTypeDefinition && type.TypeDefinition.IsArrayType)
        return GetArrayType(type, typeParametersFromContext, psiModule);

      var qualifiedName = GetQualifiedName(type);
      if (qualifiedName == null)
        return null;

      var declaredType = TypeFactory.CreateTypeByCLRName(qualifiedName, psiModule);
      var typeElement = declaredType.GetTypeElement();
      if (typeElement == null)
        return null;

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
      Assertion.Assert(typeParams.Count == fsTypes.Count, "typeParameters.Count == fsTypes.Count");
      var typeArgs = new IType[fsTypes.Count];
      for (var i = 0; i < fsTypes.Count; i++)
        typeArgs[i] = GetTypeArgumentType(fsTypes[i], typeParams[i].Type(), typeParametersFromContext, psiModule);

      return TypeFactory.CreateType(typeElement, typeArgs);
    }

    [CanBeNull]
    private static IType GetTypeArgumentType([NotNull] FSharpType arg, [CanBeNull] IType typeParam,
      [CanBeNull] IList<ITypeParameter> typeParametersFromContext, [NotNull] IPsiModule psiModule)
    {
      return arg.IsGenericParameter
        ? FindTypeParameterByName(arg, typeParametersFromContext) ?? typeParam
        : GetType(arg, typeParametersFromContext, psiModule);
    }

    [CanBeNull]
    private static IType FindTypeParameterByName([NotNull] FSharpType type,
      [CanBeNull] IEnumerable<ITypeParameter> typeParameters)
    {
      Assertion.Assert(type.IsGenericParameter, "type.IsGenericParameter");
      var typeParam = typeParameters?.FirstOrDefault(
        typeParameter => typeParameter.ShortName == type.GenericParameter.DisplayName);

      return typeParam != null ? TypeFactory.CreateType(typeParam) : null;
    }

    [NotNull]
    private static FSharpType GetAbbreviatedType([NotNull] FSharpType fsType)
    {
      while (fsType.IsAbbreviation)
        fsType = fsType.AbbreviatedType;

      return fsType;
    }

    [CanBeNull]
    private static IDeclaredType GetDeclaredType([NotNull] FSharpEntity entity, [NotNull] IPsiModule psiModule)
    {
      if (((FSharpSymbol) entity).DeclarationLocation == null)
        return null;

      if (entity.IsFSharpAbbreviation)
        entity = GetAbbreviatedType(entity.AbbreviatedType).TypeDefinition;

      return TypeFactory.CreateTypeByCLRName(GetQualifiedName(entity), psiModule);
    }

    [CanBeNull]
    private static INamespace GetDeclaredNamespace([NotNull] FSharpEntity entity, IPsiModule psiModule)
    {
      var nsName = entity.CompiledName;
      var containingName = entity.Namespace?.Value;
      var fullName = containingName != null ? containingName + "." + nsName : nsName;
      var symbolScope = psiModule.GetPsiServices().Symbols.GetSymbolScope(psiModule, true, true);
      return symbolScope.GetElementsByQualifiedName(fullName).FirstOrDefault() as INamespace;
    }

    [CanBeNull]
    public static IClrDeclaredElement GetDeclaredElement([NotNull] FSharpSymbol symbol, [NotNull] IPsiModule psiModule)
    {
      var entity = symbol as FSharpEntity;
      if (entity != null)
      {
        if (entity.IsNamespace) return GetDeclaredNamespace(entity, psiModule);
        return GetDeclaredType(entity, psiModule)?.GetTypeElement();
      }

      var unionCase = symbol as FSharpUnionCase;
      if (unionCase != null)
      {
        // here are two distinct cases:
        // * case with fields, inherited class
        // * case without fields, singleton property
      }

      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      if (mfv != null)
      {
        try
        {
          var type = GetContainingType(mfv, psiModule);
          var members = type?.GetTypeElement()?.EnumerateMembers(mfv.CompiledName, true);
          return members?.FirstOrDefault(); // todo: check overloads
        }
        catch (Exception) // todo: remove this check and find element properly
        {
          return null;
        }
      }
      return null;
    }

    [CanBeNull]
    private static IDeclaredType GetContainingType([NotNull] FSharpMemberOrFunctionOrValue mfv,
      [NotNull] IPsiModule psiModule)
    {
      try
      {
        return GetDeclaredType(mfv.EnclosingEntity, psiModule);
      }
      catch (InvalidOperationException)
      {
        // element is local to some member and is not stored in R# caches
        return null;
      }
    }

    [CanBeNull]
    public static FSharpSymbol GetFSharpSymbolFromFakeElement(IDeclaredElement element)
    {
      return (element as FSharpFakeElementFromReference)?.Symbol;
    }
  }
}