using System;
using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Logging;
using static FSharp.Compiler.ExtensionTyping;
using Range = FSharp.Compiler.Text.Range;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  /// <summary>
  /// Map FSharpType elements (as seen by FSharp.Compiler.Service) to IType types.
  /// </summary>
  public static class FcsTypeMappingUtil
  {
    [CanBeNull]
    public static IDeclaredType MapBaseType([NotNull] this FSharpEntity entity, IList<ITypeParameter> typeParams,
      [NotNull] IPsiModule psiModule) =>
      entity.BaseType?.Value is { } baseType
        ? MapType(baseType, typeParams, psiModule) as IDeclaredType
        : TypeFactory.CreateUnknownType(psiModule);

    [NotNull]
    public static IEnumerable<IDeclaredType> GetSuperTypes([NotNull] this FSharpEntity entity,
      IList<ITypeParameter> typeParams, [NotNull] IPsiModule psiModule)
    {
      var interfaces = entity.DeclaredInterfaces;
      var types = new List<IDeclaredType>(interfaces.Count + 1);
      foreach (var entityInterface in interfaces)
        if (MapType(entityInterface, typeParams, psiModule) is IDeclaredType declaredType)
          types.Add(declaredType);

      var baseType = MapBaseType(entity, typeParams, psiModule);
      if (baseType != null)
        types.Add(baseType);

      return types;
    }

    [CanBeNull]
    public static IClrTypeName GetClrName([NotNull] this FSharpEntity entity)
    {
      if (entity.IsArrayType)
        return PredefinedType.ARRAY_FQN;

      try
      {
        // `Replace` workarounds fix for https://github.com/dotnet/fsharp/issues/9.
        return new ClrTypeName(entity.QualifiedBaseName.Replace(@"\,", ","));
      }
      catch (Exception e)
      {
        Logger.LogMessage(LoggingLevel.WARN, "Could not map FSharpEntity: {0}", entity);
        Logger.LogExceptionSilently(e);
        return null;
      }
    }

    private static bool HasGenericTypeParams([NotNull] FSharpType fcsType)
    {
      if (fcsType.IsGenericParameter)
        return true;

      foreach (var typeArg in fcsType.GenericArguments)
        if (typeArg.IsGenericParameter || HasGenericTypeParams(typeArg))
          return true;

      return false;
    }

    [CanBeNull]
    private static FSharpType GetStrippedType([NotNull] FSharpType fcsType)
    {
      try
      {
        return fcsType.StrippedType;
      }
      catch (Exception e)
      {
        Logger.LogMessage(LoggingLevel.WARN, "Getting stripped type: {0}", fcsType);
        Logger.LogExceptionSilently(e);
        return null;
      }
    }

    [NotNull]
    public static IType MapType([NotNull] this FSharpType fcsType, [NotNull] IList<ITypeParameter> typeParams,
      [NotNull] IPsiModule psiModule, bool isFromMethod = false, bool isFromReturn = false)
    {
      var type = GetStrippedType(fcsType);
      if (type == null || type.IsUnresolved)
        return TypeFactory.CreateUnknownType(psiModule);

      // F# 4.0 specs 18.1.3
      try
      {
        // todo: check type vs fcsType
        if (isFromMethod && type.IsNativePtr && !HasGenericTypeParams(fcsType))
        {
          var argType = GetSingleTypeArgument(fcsType, typeParams, psiModule, true);
          return TypeFactory.CreatePointerType(argType);
        }
      }
      catch (Exception e)
      {
        Logger.LogMessage(LoggingLevel.WARN, "Could not map pointer type: {0}", fcsType);
        Logger.LogExceptionSilently(e);
      }

      if (isFromReturn && type.IsUnit)
        return psiModule.GetPredefinedType().Void;

      if (type.IsGenericParameter)
        return GetTypeParameterByName(type, typeParams, psiModule);

      if (!type.HasTypeDefinition)
        return TypeFactory.CreateUnknownType(psiModule);

      var entity = type.TypeDefinition;
      // F# 4.0 specs 5.1.4
      if (entity.IsArrayType)
      {
        var argType = GetSingleTypeArgument(type, typeParams, psiModule, isFromMethod);
        return TypeFactory.CreateArrayType(argType, type.TypeDefinition.ArrayRank, NullableAnnotation.Unknown);
      }

      // e.g. byref<int>, we need int
      if (entity.IsByRef)
        return MapType(type.GenericArguments[0], typeParams, psiModule, isFromMethod, isFromReturn);

      if (entity.IsProvidedAndErased)
        return entity.BaseType is { } baseType
          ? MapType(baseType.Value, typeParams, psiModule, isFromMethod, isFromReturn)
          : TypeFactory.CreateUnknownType(psiModule);

      var clrName = entity.GetClrName();

      if (clrName == null)
      {
        // bug Microsoft/visualfsharp#3532
        // e.g. byref<int>, we need int
        return entity.CompiledName == "byref`1" && entity.AccessPath == "Microsoft.FSharp.Core"
          ? MapType(type.GenericArguments[0], typeParams, psiModule, isFromMethod, isFromReturn)
          : TypeFactory.CreateUnknownType(psiModule);
      }

      if (entity.IsProvidedAndGenerated &&
          ProvidedTypesResolveUtil.TryGetProvidedType(psiModule, clrName, out var providedType))
        return MapType(providedType, psiModule);

      var declaredType = clrName.CreateTypeByClrName(psiModule);
      var genericArgs = type.GenericArguments;
      if (genericArgs.IsEmpty())
        return declaredType;

      var typeElement = declaredType.GetTypeElement();
      return typeElement != null
        ? GetTypeWithSubstitution(typeElement, genericArgs, typeParams, psiModule, isFromMethod)
        : TypeFactory.CreateUnknownType(psiModule);
    }

    [NotNull]
    public static IType MapType([NotNull] this ProvidedType providedType, IPsiModule module)
    {
      if (providedType is not IProxyProvidedType proxyProvidedType)
      {
        Assertion.Fail("ProvidedType should be IProxyProvidedType");
        return TypeFactory.CreateUnknownType(module);
      }

      if (proxyProvidedType.IsCreatedByProvider &&
          providedType.DeclaringType is ProxyProvidedTypeWithContext declaringType)
      {
        var declaringTypeIType = declaringType.MapType(module);

        if (declaringTypeIType.GetTypeElement() is { } x)
          return TypeFactory.CreateType(new FSharpGenerativeProvidedNestedClass(providedType, module, x));

        var recoveredTypeElement = module
          .GetSymbolScope(false)
          .GetTypeElementsByCLRName(declaringType.GetClrName())
          .FirstOrDefault(t => t is FSharpClassOrProvidedTypeAbbreviation { IsProvidedAndGenerated: true });

        Assertion.AssertNotNull(recoveredTypeElement,
          "SymbolScope must contain provided and generated FSharpClassOrProvidedTypeAbbreviation ");

        return TypeFactory.CreateType(
          new FSharpGenerativeProvidedNestedClass(providedType, module, recoveredTypeElement));
      }

      if (providedType.IsArray)
        return TypeFactory.CreateArrayType(providedType.GetElementType().MapType(module), providedType.GetArrayRank(),
          NullableAnnotation.Unknown);

      if (providedType.IsPointer)
        return TypeFactory.CreatePointerType(providedType.GetElementType().MapType(module));

      if (!providedType.IsGenericType)
        return TypeFactory.CreateTypeByCLRName(proxyProvidedType.GetClrName(), NullableAnnotation.Unknown, module);

      if (providedType.GetGenericTypeDefinition() is not IProxyProvidedType genericTypeDefinition)
      {
        Assertion.Fail("providedType.GetGenericTypeDefinition() should be IProxyProvidedType");
        return TypeFactory.CreateUnknownType(module);
      }

      var typeDefinition =
        TypeFactory.CreateTypeByCLRName(genericTypeDefinition.GetClrName(), NullableAnnotation.Unknown, module);

      var genericProvidedArgs = providedType.GetGenericArguments();
      var genericTypes = new IType[genericProvidedArgs.Length];

      for (var i = 0; i < genericProvidedArgs.Length; i++)
        genericTypes[i] = MapType(genericProvidedArgs[i], module);

      var typeElement = typeDefinition.GetTypeElement();

      return typeElement != null
        ? TypeFactory.CreateType(typeElement, genericTypes)
        : TypeFactory.CreateUnknownType(module);
    }

    // todo: get type parameters for local bindings
    public static IType MapType([NotNull] this FSharpType fcsType, [NotNull] ITreeNode context)
    {
      var typeMemberDeclaration = context.GetContainingNode<ITypeMemberDeclaration>();
      var typeParametersOwner = typeMemberDeclaration?.DeclaredElement as IFSharpTypeParametersOwner;
      var typeParameters = typeParametersOwner?.AllTypeParameters ?? EmptyList<ITypeParameter>.Instance;
      return MapType(fcsType, typeParameters, context.GetPsiModule());
    }

    [NotNull]
    private static IType GetSingleTypeArgument([NotNull] FSharpType fcsType, IList<ITypeParameter> typeParams,
      IPsiModule psiModule, bool isFromMethod)
    {
      var genericArgs = fcsType.GenericArguments;
      Assertion.Assert(genericArgs.Count == 1);
      return GetTypeArgumentType(genericArgs[0], typeParams, psiModule, isFromMethod);
    }

    [NotNull]
    private static IDeclaredType GetTypeWithSubstitution([NotNull] ITypeElement typeElement,
      IList<FSharpType> fcsTypeArgs, [NotNull] IList<ITypeParameter> typeParams, [NotNull] IPsiModule psiModule,
      bool isFromMethod)
    {
      var typeParamsCount = typeElement.GetAllTypeParameters().Count;
      var typeArgs = new IType[typeParamsCount];
      for (var i = 0; i < typeParamsCount; i++)
        typeArgs[i] = GetTypeArgumentType(fcsTypeArgs[i], typeParams, psiModule, isFromMethod);

      return TypeFactory.CreateType(typeElement, typeArgs);
    }

    [NotNull]
    private static IType GetTypeArgumentType([NotNull] FSharpType arg, [NotNull] IList<ITypeParameter> typeParams,
      [NotNull] IPsiModule psiModule, bool isFromMethod) =>
      arg.IsGenericParameter
        ? GetTypeParameterByName(arg, typeParams, psiModule)
        : MapType(arg, typeParams, psiModule, isFromMethod);

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

    public static ParameterKind MapParameterKind([NotNull] this FSharpParameter param)
    {
      var fcsType = param.Type;
      if (fcsType.HasTypeDefinition && fcsType.TypeDefinition is var entity && entity.IsByRef)
      {
        if (param.IsOut || entity.LogicalName == "outref`1")
          return ParameterKind.OUTPUT;
        if (param.IsInArg || entity.LogicalName == "inref`1")
          return ParameterKind.INPUT;

        return ParameterKind.REFERENCE;
      }

      return ParameterKind.VALUE;
    }

    public static bool TryGetFcsRange(this ITreeNode treeNode, out Range range)
    {
      range = default;

      var sourceFile = treeNode.GetSourceFile();
      if (sourceFile == null) return false;

      range = treeNode.GetDocumentRange().ToFcsRange(sourceFile.GetLocation());
      return true;
    }

    private static FSharpCheckFileResults GetCheckResults(this IFSharpTreeNode fsTreeNode, string opName) =>
      fsTreeNode.FSharpFile.GetParseAndCheckResults(true, opName)?.Value?.CheckResults;

    [CanBeNull]
    public static FSharpType TryGetFcsType([NotNull] this IFSharpTreeNode treeNode)
    {
      var checkResults = treeNode.GetCheckResults(nameof(TryGetFcsType));
      if (checkResults == null) return null;

      return treeNode.TryGetFcsRange(out var range)
        ? checkResults.GetTypeOfExpression(range)?.Value
        : null;
    }

    [CanBeNull]
    public static FSharpDisplayContext TryGetFcsDisplayContext([NotNull] this IFSharpTreeNode treeNode)
    {
      var checkResults = treeNode.GetCheckResults(nameof(TryGetFcsDisplayContext));
      if (checkResults == null) return null;

      return treeNode.TryGetFcsRange(out var range)
        ? checkResults.GetExpressionDisplayContext(range)?.Value
        : null;
    }

    [NotNull]
    public static IType GetExpressionTypeFromFcs([NotNull] this IFSharpTreeNode fsTreeNode)
    {
      var fsharpType = TryGetFcsType(fsTreeNode);
      return fsharpType != null
        ? fsharpType.MapType(fsTreeNode)
        : TypeFactory.CreateUnknownType(fsTreeNode.GetPsiModule());
    }

    [NotNull]
    public static IDeclaredType CreateTypeByClrName([NotNull] this IClrTypeName clrTypeName,
      [NotNull] IPsiModule psiModule) =>
      TypeFactory.CreateTypeByCLRName(clrTypeName, NullableAnnotation.Unknown, psiModule);
  }
}
