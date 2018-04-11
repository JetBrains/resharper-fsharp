using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  /// <summary>
  /// Map FSharpType elements (as seen by FSharp.Compiler.Service) to IType types.
  /// </summary>
  public static class FSharpTypesUtil
  {
    private const string ArrayClrName = "System.Array";

    private static readonly object ourFcsLock = new object();

    [CanBeNull]
    public static IDeclaredType GetBaseType([NotNull] FSharpEntity entity,
      IList<ITypeParameter> typeParamsFromContext, [NotNull] IPsiModule psiModule)
    {
      var fsBaseType = GetFSharpBaseType(entity);
      return fsBaseType != null
        ? GetType(fsBaseType.Value, typeParamsFromContext, psiModule) as IDeclaredType
        : TypeFactory.CreateUnknownType(psiModule);
    }

    private static FSharpOption<FSharpType> GetFSharpBaseType([NotNull] FSharpEntity entity)
    {
      lock (ourFcsLock)
        return entity.BaseType;
    }

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
    public static string GetClrName([NotNull] FSharpEntity entity)
    {
      // F# 4.0 specs 5.1.4
      if (entity.IsArrayType)
        return ArrayClrName;

      // qualified name may include assembly name, public key, etc and separated with comma, e.g. for unit it returns
      // "Microsoft.FSharp.Core.Unit, FSharp.Core, Version=4.4.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
      try
      {
        return entity.QualifiedBaseName;
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
    [CanBeNull]
    public static IType GetType([NotNull] FSharpType fsType, [NotNull] ITypeMemberDeclaration typeMemberDeclaration,
      [NotNull] IPsiModule psiModule)
    {
      return GetType(fsType, GetOuterTypeParameters(typeMemberDeclaration), psiModule);
    }

    /// <summary>
    /// Get type from a context of some declaration, possibly containing type parameters declarations.
    /// Overload for method context.
    /// </summary>
    [CanBeNull]
    public static IType GetType([NotNull] FSharpType fsType,
      [NotNull] ITypeMemberDeclaration methodDeclaration, [NotNull] IList<ITypeParameter> methodTypeParams,
      [NotNull] IPsiModule psiModule, bool isFromReturn)
    {
      var typeParametersFromType = GetOuterTypeParameters(methodDeclaration);
      var typeParamsFromContext = typeParametersFromType.Prepend(methodTypeParams).ToIList();
      return GetType(fsType, typeParamsFromContext, psiModule, true, isFromReturn);
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
      if (fsType.IsGenericParameter) return true;
      foreach (var typeArg in fsType.GenericArguments)
        if (typeArg.IsGenericParameter || HasGenericTypeParams(typeArg)) return true;

      return false;
    }

    [CanBeNull]
    private static FSharpType GetStrippedType([NotNull] FSharpType fsType)
    {
      try
      {
        return fsType.StrippedType;
      }
      catch (Exception e)
      {
        Logger.LogMessage(LoggingLevel.WARN, "Error mapping type {0}", fsType);
        Logger.LogExceptionSilently(e);
        return null;
      }
    }

    [CanBeNull]
    public static IType GetType([NotNull] FSharpType fsType, [CanBeNull] IList<ITypeParameter> typeParamsFromContext,
      [NotNull] IPsiModule psiModule, bool isFromMethodSig = false, bool isFromReturn = false)
    {
      var type = GetStrippedType(fsType);
      if (type?.IsUnresolved ?? true)
        return TypeFactory.CreateUnknownType(psiModule);

      // F# 4.0 specs 18.1.3
      if (isFromMethodSig && type.IsNativePtr && !HasGenericTypeParams(fsType))
      {
        try
        {
          var argType = GetSingleTypeArgument(fsType, typeParamsFromContext, psiModule, true);
          return TypeFactory.CreatePointerType(argType);
        }
        catch (Exception e)
        {
          Logger.LogMessage(LoggingLevel.WARN, "Could not map pointer type: {0}", fsType);
          Logger.LogExceptionSilently(e);
        }
      }

      if (type.IsGenericParameter)
        return FindTypeParameterByName(type, typeParamsFromContext, psiModule);

      if (!type.HasTypeDefinition)
        return TypeFactory.CreateUnknownType(psiModule);

      var entity = type.TypeDefinition;
      // F# 4.0 specs 5.1.4
      if (entity.IsArrayType)
      {
        var argType = GetSingleTypeArgument(type, typeParamsFromContext, psiModule, isFromMethodSig);
        return TypeFactory.CreateArrayType(argType, type.TypeDefinition.ArrayRank);
      }

      // e.g. byref<int>, we need int
      if (entity.IsByRef)
        return GetType(type.GenericArguments[0], typeParamsFromContext, psiModule, isFromMethodSig, isFromReturn);

      if (entity.IsProvidedAndErased)
      {
        var fsBaseType = GetFSharpBaseType(entity);
        if (fsBaseType != null)
          return GetType(fsBaseType.Value, typeParamsFromContext, psiModule, isFromMethodSig, isFromReturn);
      }

      var clrName = GetClrName(entity);
      if (clrName == null)
      {
        // bug Microsoft/visualfsharp#3532
        // e.g. byref<int>, we need int
        return entity.CompiledName == "byref`1" && entity.AccessPath == "Microsoft.FSharp.Core"
          ? GetType(type.GenericArguments[0], typeParamsFromContext, psiModule, isFromMethodSig, isFromReturn)
          : TypeFactory.CreateUnknownType(psiModule);
      }

      var declaredType = TypeFactory.CreateTypeByCLRName(clrName, psiModule);
      var typeElement = declaredType.GetTypeElement();
      if (typeElement == null)
        return TypeFactory.CreateUnknownType(psiModule);

      var args = type.GenericArguments;
      return args.Count != 0
        ? GetTypeWithSubstitution(typeElement, args, typeParamsFromContext, psiModule, isFromMethodSig) ??
          declaredType
        : declaredType;
    }

    [NotNull]
    private static IType GetSingleTypeArgument([NotNull] FSharpType fsType,
      IList<ITypeParameter> typeParamsFromContext, IPsiModule psiModule, bool isFromMethodSig)
    {
      Assertion.Assert(fsType.GenericArguments.Count == 1, "fsType.GenericArguments.Count == 1");
      return GetTypeArgumentType(fsType.GenericArguments[0], null, typeParamsFromContext, psiModule,
               isFromMethodSig) ??
             TypeFactory.CreateUnknownType(psiModule);
    }

    [CanBeNull]
    private static IDeclaredType GetTypeWithSubstitution([NotNull] ITypeElement typeElement, IList<FSharpType> fsTypes,
      [CanBeNull] IList<ITypeParameter> typeParamsFromContext, [NotNull] IPsiModule psiModule,
      bool isFromMethodSig)
    {
      var typeParams = typeElement.GetAllTypeParameters().Reverse().ToIList();
      var typeArgs = new IType[typeParams.Count];
      for (var i = 0; i < typeParams.Count; i++)
      {
        var typeArg = GetTypeArgumentType(fsTypes[i], typeParams[i]?.Type(), typeParamsFromContext, psiModule,
          isFromMethodSig);
        if (typeArg == null)
          return TypeFactory.CreateUnknownType(psiModule);

        typeArgs[i] = typeArg;
      }

      return TypeFactory.CreateType(typeElement, typeArgs);
    }

    [CanBeNull]
    private static IType GetTypeArgumentType([NotNull] FSharpType arg, [CanBeNull] IType typeParam,
      [CanBeNull] IList<ITypeParameter> typeParamsFromContext, [NotNull] IPsiModule psiModule, bool isFromMethodSig)
    {
      return arg.IsGenericParameter
        ? FindTypeParameterByName(arg, typeParamsFromContext, psiModule) ?? typeParam
        : GetType(arg, typeParamsFromContext, psiModule, isFromMethodSig);
    }

    // todo: remove and add API to FCS.FSharpParameter
    [CanBeNull]
    private static IType FindTypeParameterByName([NotNull] FSharpType type,
      [CanBeNull] IEnumerable<ITypeParameter> typeParameters, [NotNull] IPsiModule psiModule)
    {
      var typeParam = typeParameters?.FirstOrDefault(p => p.ShortName == type.GenericParameter.DisplayName);
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