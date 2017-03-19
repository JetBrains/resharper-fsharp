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
        ? GetDeclaredType(entity.BaseType.Value, typeParametersFromContext, psiModule)
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
        var declaredType = GetDeclaredType(entityInterface, typeParametersFromContext, psiModule);
        if (declaredType != null) types.Add(declaredType);
      }
      var baseType = GetBaseType(entity, typeParametersFromContext, psiModule);
      if (baseType != null) types.Add(baseType);
      return types;
    }

    [CanBeNull]
    public static string GetQualifiedName([NotNull] FSharpEntity entity)
    {
      // sometimes name includes assembly name, public key, etc and separated with comma
      return entity.QualifiedName.SubstringBefore(",");
    }

    /// <summary>
    /// Get declared type from a context of some declaration, possibly containing type parameters declarations.
    /// </summary>
    public static IDeclaredType GetDeclaredType([NotNull] FSharpType fsType,
      [NotNull] ITypeMemberDeclaration typeMemberDeclaration, [NotNull] IPsiModule psiModule)
    {
      var typeDeclaration = typeMemberDeclaration.GetContainingTypeDeclaration();
      var typeParameters = typeDeclaration?.DeclaredElement?.TypeParameters; // todo obj type member type params
      return GetDeclaredType(fsType, typeParameters, psiModule);
    }

    // todo: tuple types
    [CanBeNull]
    public static IDeclaredType GetDeclaredType([NotNull] FSharpType fsType,
      [CanBeNull] IList<ITypeParameter> typeParametersFromContext, [NotNull] IPsiModule psiModule)
    {
      while (fsType.IsAbbreviation)
        fsType = fsType.AbbreviatedType;

      var qualifiedName = GetQualifiedName(fsType.TypeDefinition);
      if (qualifiedName == null)
        return null;

      var declaredType = TypeFactory.CreateTypeByCLRName(qualifiedName, psiModule);
      var typeElement = declaredType.GetTypeElement();
      if (typeElement == null)
        return null;

      var args = fsType.GenericArguments;
      return args.Count != 0
        ? GetTypeWithSubstitution(typeElement, args, typeParametersFromContext, psiModule) ?? declaredType
        : declaredType;
    }

    [CanBeNull]
    private static IDeclaredType GetTypeWithSubstitution([NotNull] ITypeElement typeElement, IList<FSharpType> fsTypes,
      [CanBeNull] IList<ITypeParameter> typeParametersFromContext, [NotNull] IPsiModule psiModule)
    {
      var typeParams = typeElement.TypeParameters;
      Assertion.Assert(typeParams.Count == fsTypes.Count, "typeParameters.Count == fsTypes.Count");
      var typeArgs = new IType[fsTypes.Count];
      for (var i = 0; i < fsTypes.Count; i++)
      {
        var arg = fsTypes[i];
        typeArgs[i] = arg.IsGenericParameter
          ? FindTypeParameterByName(arg, typeParametersFromContext) ?? typeParams[i].Type()
          : GetDeclaredType(arg, typeParametersFromContext, psiModule);
      }

      return TypeFactory.CreateType(typeElement, typeArgs);
    }

    [CanBeNull]
    public static IType FindTypeParameterByName([NotNull] FSharpType type,
      [CanBeNull] IEnumerable<ITypeParameter> typeParameters)
    {
      Assertion.Assert(type.IsGenericParameter, "type.IsGenericParameter");
      var typeParam = typeParameters?.FirstOrDefault(
        typeParameter => typeParameter.ShortName == type.GenericParameter.DisplayName);

      return typeParam != null ? TypeFactory.CreateType(typeParam) : null;
    }

    [CanBeNull]
    public static IDeclaredType GetDeclaredType([NotNull] FSharpEntity entity, [NotNull] IPsiModule psiModule)
    {
      if (((FSharpSymbol) entity).DeclarationLocation == null) return null;

      if (entity.IsFSharpAbbreviation)
      {
        var type = entity.AbbreviatedType;
        while (type.IsAbbreviation) type = type.AbbreviatedType;
        entity = type.TypeDefinition;
      }
      var qualifiedName = GetQualifiedName(entity);
      return qualifiedName != null ? TypeFactory.CreateTypeByCLRName(qualifiedName, psiModule) : null;
    }

    public static INamespace GetDeclaredNamespace([NotNull] FSharpEntity entity, IPsiModule psiModule)
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