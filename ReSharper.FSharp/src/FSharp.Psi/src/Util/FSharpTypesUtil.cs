using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  /// <summary>
  /// Map FSharpType elements (as seen by FSharp.Compiler.Service) to IType types.
  /// </summary>
  public static class FSharpTypesUtil
  {
    [CanBeNull]
    public static IDeclaredType GetBaseType([NotNull] FSharpEntity entity,
      IList<ITypeParameter> typeParamsFromContext, [NotNull] IPsiModule psiModule) =>
      entity.BaseType is var fsBaseType && fsBaseType != null
        ? GetType(fsBaseType.Value, typeParamsFromContext, psiModule) as IDeclaredType
        : TypeFactory.CreateUnknownType(psiModule);

    [NotNull]
    public static IEnumerable<IDeclaredType> GetSuperTypes([NotNull] FSharpEntity entity,
      IList<ITypeParameter> typeParamsFromContext, [NotNull] IPsiModule psiModule)
    {
      var interfaces = entity.DeclaredInterfaces;
      var types = new List<IDeclaredType>(interfaces.Count + 1);
      foreach (var entityInterface in interfaces)
        if (GetType(entityInterface, typeParamsFromContext, psiModule) is IDeclaredType declaredType)
          types.Add(declaredType);

      var baseType = GetBaseType(entity, typeParamsFromContext, psiModule);
      if (baseType != null) types.Add(baseType);
      return types;
    }

    [CanBeNull]
    public static IClrTypeName GetClrName([NotNull] FSharpEntity entity)
    {
      if (entity.IsArrayType)
        return PredefinedType.ARRAY_FQN;

      try
      {
        return new ClrTypeName(entity.QualifiedBaseName);
      }
      catch (Exception e)
      {
        Logger.LogMessage(LoggingLevel.WARN, "Could not map FSharpEntity: {0}", entity);
        Logger.LogExceptionSilently(e);
        return null;
      }
    }

    /// <summary>
    /// Get type from a context of some declaration, possibly containing type parameters declarations.
    /// </summary>
    [NotNull]
    public static IType GetType([NotNull] FSharpType fsType, [NotNull] ITypeMemberDeclaration typeMemberDeclaration,
      [NotNull] IPsiModule psiModule)
    {
      try
      {
        return GetType(fsType, GetOuterTypeParameters(typeMemberDeclaration), psiModule);
      }
      catch (ErrorLogger.UnresolvedPathReferenceNoRange)
      {
        return TypeFactory.CreateUnknownType(psiModule);
      }
    }

    /// <summary>
    /// Get type from a context of some declaration, possibly containing type parameters declarations.
    /// Overload for method context.
    /// </summary>
    [NotNull]
    public static IType GetType([NotNull] FSharpType fsType,
      [NotNull] ITypeMemberDeclaration methodDeclaration, [NotNull] IList<ITypeParameter> methodTypeParams,
      [NotNull] IPsiModule psiModule, bool isFromReturn)
    {
      var typeParametersFromType = GetOuterTypeParameters(methodDeclaration);
      var typeParamsFromContext = typeParametersFromType.Prepend(methodTypeParams).ToIList();
      try
      {
        return GetType(fsType, typeParamsFromContext, psiModule, true, isFromReturn);
      }
      catch (ErrorLogger.UnresolvedPathReferenceNoRange)
      {
        return TypeFactory.CreateUnknownType(psiModule);
      }
    }

    [NotNull]
    private static IList<ITypeParameter> GetOuterTypeParameters(ITypeMemberDeclaration typeMemberDeclaration)
    {
      var typeDeclaration = typeMemberDeclaration.GetContainingTypeDeclaration();
      var parameters = typeDeclaration?.DeclaredElement?.GetAllTypeParameters();

      return parameters?.ResultingList() ??
             EmptyList<ITypeParameter>.Instance;
    }

    private static bool HasGenericTypeParams([NotNull] FSharpType fsType)
    {
      if (fsType.IsGenericParameter)
        return true;

      foreach (var typeArg in fsType.GenericArguments)
        if (typeArg.IsGenericParameter || HasGenericTypeParams(typeArg))
          return true;

      return false;
    }

    [CanBeNull]
    private static FSharpType GetStrippedType([NotNull] FSharpType type)
    {
      try
      {
        return type.StrippedType;
      }
      catch (Exception e)
      {
        Logger.LogMessage(LoggingLevel.WARN, "Getting stripped type: {0}", type);
        Logger.LogExceptionSilently(e);
        return null;
      }
    }

    [NotNull]
    public static IType GetType([NotNull] FSharpType fsType, [NotNull] IList<ITypeParameter> typeParams,
      [NotNull] IPsiModule psiModule, bool isFromMethod = false, bool isFromReturn = false)
    {
      var type = GetStrippedType(fsType);
      if (type == null || type.IsUnresolved)
        return TypeFactory.CreateUnknownType(psiModule);

      // F# 4.0 specs 18.1.3
      try
      {
        // todo: check type vs fsType
        if (isFromMethod && type.IsNativePtr && !HasGenericTypeParams(fsType))
        {
          var argType = GetSingleTypeArgument(fsType, typeParams, psiModule, true);
          return TypeFactory.CreatePointerType(argType);
        }
      }
      catch (Exception e)
      {
        Logger.LogMessage(LoggingLevel.WARN, "Could not map pointer type: {0}", fsType);
        Logger.LogExceptionSilently(e);
      }

      if (type.IsGenericParameter)
        return GetTypeParameterByName(type, typeParams, psiModule);

      if (!type.HasTypeDefinition)
        return TypeFactory.CreateUnknownType(psiModule);

      var entity = type.TypeDefinition;
      // F# 4.0 specs 5.1.4
      if (entity.IsArrayType)
      {
        var argType = GetSingleTypeArgument(type, typeParams, psiModule, isFromMethod);
        return TypeFactory.CreateArrayType(argType, type.TypeDefinition.ArrayRank);
      }

      // e.g. byref<int>, we need int
      if (entity.IsByRef)
        return GetType(type.GenericArguments[0], typeParams, psiModule, isFromMethod, isFromReturn);

      if (entity.IsProvidedAndErased)
        return entity.BaseType is var baseType && baseType != null
          ? GetType(baseType.Value, typeParams, psiModule, isFromMethod, isFromReturn)
          : TypeFactory.CreateUnknownType(psiModule);

      var clrName = GetClrName(entity);
      if (clrName == null)
      {
        // bug Microsoft/visualfsharp#3532
        // e.g. byref<int>, we need int
        return entity.CompiledName == "byref`1" && entity.AccessPath == "Microsoft.FSharp.Core"
          ? GetType(type.GenericArguments[0], typeParams, psiModule, isFromMethod, isFromReturn)
          : TypeFactory.CreateUnknownType(psiModule);
      }

      var declaredType = TypeFactory.CreateTypeByCLRName(clrName, psiModule);
      var genericArgs = type.GenericArguments;
      if (genericArgs.IsEmpty())
        return declaredType;

      var typeElement = declaredType.GetTypeElement();
      return typeElement != null
        ? GetTypeWithSubstitution(typeElement, genericArgs, typeParams, psiModule, isFromMethod)
        : TypeFactory.CreateUnknownType(psiModule);
    }

    [NotNull]
    private static IType GetSingleTypeArgument([NotNull] FSharpType fsType, IList<ITypeParameter> typeParams,
      IPsiModule psiModule, bool isFromMethod)
    {
      var genericArgs = fsType.GenericArguments;
      Assertion.Assert(genericArgs.Count == 1, "genericArgs.Count == 1");
      return GetTypeArgumentType(genericArgs[0], typeParams, psiModule, isFromMethod);
    }

    [NotNull]
    private static IDeclaredType GetTypeWithSubstitution([NotNull] ITypeElement typeElement, IList<FSharpType> fsTypes,
      [NotNull] IList<ITypeParameter> typeParams, [NotNull] IPsiModule psiModule, bool isFromMethod)
    {
      var typeParamsCount = typeElement.GetAllTypeParameters().Count;
      var typeArgs = new IType[typeParamsCount];
      for (var i = 0; i < typeParamsCount; i++)
      {
        var typeArg = GetTypeArgumentType(fsTypes[i], typeParams, psiModule, isFromMethod);
        typeArgs[i] = typeArg;
      }

      return TypeFactory.CreateType(typeElement, typeArgs);
    }

    [NotNull]
    private static IType GetTypeArgumentType([NotNull] FSharpType arg, [NotNull] IList<ITypeParameter> typeParams,
      [NotNull] IPsiModule psiModule, bool isFromMethod) =>
      arg.IsGenericParameter
        ? GetTypeParameterByName(arg, typeParams, psiModule)
        : GetType(arg, typeParams, psiModule, isFromMethod);

    // todo: remove and add API to FCS.FSharpParameter
    [NotNull]
    private static IType GetTypeParameterByName([NotNull] FSharpType type,
      [NotNull] IList<ITypeParameter> typeParameters, [NotNull] IPsiModule psiModule)
    {
      var paramName = type.GenericParameter.Name;
      var typeParam = typeParameters.FirstOrDefault(p => p.ShortName == paramName);
      return typeParam != null
        ? TypeFactory.CreateType(typeParam)
        : TypeFactory.CreateUnknownType(psiModule);
    }

    public static ParameterKind GetParameterKind([NotNull] FSharpParameter param)
    {
      var fsType = param.Type;
      if (fsType.HasTypeDefinition && fsType.TypeDefinition.IsByRef)
        return param.IsOut ? ParameterKind.OUTPUT : ParameterKind.REFERENCE;
      return ParameterKind.VALUE;
    }
  }
}
