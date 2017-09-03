using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Extension;
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
    private const string IntPtrClrName = "System.IntPtr";
    private const string TupleClrName = "System.Tuple`";
    private const string ValueTupleClrName = "System.ValueTuple`";
    private const string FSharpCoreNamespace = "Microsoft.FSharp.Core";
    private const string FSharpFuncClrName = "Microsoft.FSharp.Core.FSharpFunc`";
    private const string UnitClrName = "Microsoft.FSharp.Core.Unit";
    private const string NativeptrLogicalName = "nativeptr`1";

    private const string MeasureFloat = "Microsoft.FSharp.Core.float`1";
    private const string MeasureInt = "Microsoft.FSharp.Core.int`1";
    private const string MeasureFloat32 = "Microsoft.FSharp.Core.float32`1";
    private const string MeasureDecimal = "Microsoft.FSharp.Core.decimal`1";
    private const string MeasureSByte = "Microsoft.FSharp.Core.sbyte`1";
    private const string MeasureInt16 = "Microsoft.FSharp.Core.int16`1";
    private const string MeasureInt64 = "Microsoft.FSharp.Core.int64`1";

    private static readonly object ourFcsLock = new object();

    [CanBeNull]
    public static IDeclaredType GetBaseType([NotNull] FSharpEntity entity,
      IList<ITypeParameter> typeParametersFromContext, [NotNull] IPsiModule psiModule)
    {
      var fsBaseType = GetFSharpBaseType(entity);
      return fsBaseType != null
        ? GetType(fsBaseType.Value, typeParametersFromContext, psiModule) as IDeclaredType
        : TypeFactory.CreateUnknownType(psiModule);
    }

    private static FSharpOption<FSharpType> GetFSharpBaseType([NotNull] FSharpEntity entity)
    {
      lock (ourFcsLock)
        return entity.BaseType;
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

    [CanBeNull]
    public static string GetClrName([NotNull] FSharpEntity entity)
    {
      // F# 4.0 specs 5.1.4
      if (entity.IsArrayType)
        return ArrayClrName;

      // F# 4.0 specs 18.1.3
      if (entity.IsNativePtr())
        return IntPtrClrName;

      // qualified name may include assembly name, public key, etc and separated with comma, e.g. for unit it returns
      // "Microsoft.FSharp.Core.Unit, FSharp.Core, Version=4.4.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
      try
      {
        return entity.QualifiedName.SubstringBefore(",", StringComparison.Ordinal);
      }
      catch (Exception e)
      {
        Logger.LogMessage(LoggingLevel.WARN, "Could not map FSharpEntity: {0}", entity);
        Logger.LogException(e);
        return null;
      }
    }

    [CanBeNull]
    private static string GetClrName([NotNull] FSharpType fsType)
    {
      var typeArgumentsCount = fsType.GenericArguments.Count;

      if (fsType.IsTupleType)
      {
        // F# 4.1 struct tuples
        if (fsType.IsStructTupleType)
          return ValueTupleClrName + typeArgumentsCount;

        // F# 4.0 specs 5.1.3
        return TupleClrName + typeArgumentsCount;
      }

      if (fsType.IsFunctionType)
        return FSharpFuncClrName + typeArgumentsCount;

      var entity = fsType.TypeDefinition;
      if (entity.IsProvidedAndErased)
        return null;

      return GetClrName(entity);
    }

    /// <summary>
    /// Get type from a context of some declaration, possibly containing type parameters declarations.
    /// </summary>
    [CanBeNull]
    public static IType GetType([NotNull] FSharpType fsType,
      [NotNull] ITypeMemberDeclaration typeMemberDeclaration, [NotNull] IPsiModule psiModule)
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
      [NotNull] IPsiModule psiModule)
    {
      var typeParametersFromType = GetOuterTypeParameters(methodDeclaration);
      var typeParametersFromContext = typeParametersFromType.Prepend(methodTypeParams).ToIList();
      return GetType(fsType, typeParametersFromContext, psiModule);
    }

    [NotNull]
    private static IList<ITypeParameter> GetOuterTypeParameters(ITypeMemberDeclaration typeMemberDeclaration)
    {
      var typeDeclaration = typeMemberDeclaration.GetContainingTypeDeclaration();
      var parameters = typeDeclaration?.DeclaredElement?.GetAllTypeParameters();

      return parameters?.ResultingList() ??
             EmptyList<ITypeParameter>.Instance;
    }

    [CanBeNull]
    public static IType GetType([NotNull] FSharpType fsType,
      [CanBeNull] IList<ITypeParameter> typeParametersFromContext, [NotNull] IPsiModule psiModule)
    {
      var type = GetAbbreviatedType(fsType);

      if (type.IsGenericParameter)
        return FindTypeParameterByName(type, typeParametersFromContext, psiModule);

      if (type.HasTypeDefinition)
      {
        var entity = type.TypeDefinition;

        // F# 4.0 specs 5.1.4
        if (entity.IsArrayType)
          return GetArrayType(type, typeParametersFromContext, psiModule);

        // F# 4.0 specs 18.1.3
        if (entity.IsNativePtr())
          return GetPointerType(type, typeParametersFromContext, psiModule);


        // e.g. byref<int>, we need int
        // bug Microsoft/visualfsharp#3532; keep only the first check when fixed
        if (entity.IsByRef || entity.CompiledName.Equals("byref`1", StringComparison.Ordinal) &&
            entity.AccessPath.Equals("Microsoft.FSharp.Core", StringComparison.Ordinal))
          return GetType(type.GenericArguments[0], typeParametersFromContext, psiModule);

        if (entity.IsProvidedAndErased)
        {
          var fsBaseType = GetFSharpBaseType(entity);
          if (fsBaseType != null)
            return GetType(fsBaseType.Value, typeParametersFromContext, psiModule);
        }
      }

      var clrName = GetClrName(type);
      if (clrName == null)
        return TypeFactory.CreateUnknownType(psiModule);

      var declaredType = TypeFactory.CreateTypeByCLRName(clrName, psiModule);
      var typeElement = declaredType.GetTypeElement();
      if (typeElement == null)
      {
        if (fsType.GenericArguments.Count != 0)
        {
          // todo: use ILxGen code to get compiled type
          var genArg = fsType.GenericArguments[0];
          if (genArg.HasTypeDefinition && genArg.TypeDefinition.IsMeasure ||
              genArg.IsGenericParameter && genArg.GenericParameter.IsMeasure)
          {
            if (clrName.Equals(MeasureFloat, StringComparison.Ordinal))
              return psiModule.GetPredefinedType().Double;
            if (clrName.Equals(MeasureInt, StringComparison.Ordinal))
              return psiModule.GetPredefinedType().Int;
            
            if (clrName.Equals(MeasureFloat32, StringComparison.Ordinal))
              return psiModule.GetPredefinedType().Float;
            if (clrName.Equals(MeasureDecimal, StringComparison.Ordinal))
              return psiModule.GetPredefinedType().Decimal;
            if (clrName.Equals(MeasureSByte, StringComparison.Ordinal))
              return psiModule.GetPredefinedType().Sbyte;
            if (clrName.Equals(MeasureInt16, StringComparison.Ordinal))
              return psiModule.GetPredefinedType().Short;
            if (clrName.Equals(MeasureInt64, StringComparison.Ordinal))
              return psiModule.GetPredefinedType().Long;
          }
        }
        return TypeFactory.CreateUnknownType(psiModule);
      }

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
      var argType = GetSingleTypeArgument(fsType, typeParametersFromContext, psiModule);
      return TypeFactory.CreateArrayType(argType, entity.ArrayRank);
    }

    [CanBeNull]
    private static IType GetPointerType(FSharpType fsType, IList<ITypeParameter> typeParametersFromContext,
      IPsiModule psiModule)
    {
      var argType = GetSingleTypeArgument(fsType, typeParametersFromContext, psiModule);
      return TypeFactory.CreatePointerType(argType);
    }

    [NotNull]
    private static IType GetSingleTypeArgument([NotNull] FSharpType fsType,
      IList<ITypeParameter> typeParametersFromContext, IPsiModule psiModule)
    {
      Assertion.Assert(fsType.GenericArguments.Count == 1, "fsType.GenericArguments.Count == 1");
      return GetTypeArgumentType(fsType.GenericArguments[0], null, typeParametersFromContext, psiModule) ??
             TypeFactory.CreateUnknownType(psiModule);
    }

    [CanBeNull]
    private static IDeclaredType GetTypeWithSubstitution([NotNull] ITypeElement typeElement, IList<FSharpType> fsTypes,
      [CanBeNull] IList<ITypeParameter> typeParametersFromContext, [NotNull] IPsiModule psiModule)
    {
      var typeParams = typeElement.GetAllTypeParameters().Reverse().ToIList();
      var typeArgs = new IType[typeParams.Count];
      for (var i = 0; i < typeParams.Count; i++)
      {
        var typeArg = GetTypeArgumentType(fsTypes[i], typeParams[i]?.Type(), typeParametersFromContext, psiModule);
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
      var typeParam = typeParameters?.FirstOrDefault(p => p.ShortName == type.GenericParameter.DisplayName);

      return typeParam != null
        ? TypeFactory.CreateType(typeParam)
        : TypeFactory.CreateUnknownType(psiModule);
    }

    [NotNull]
    private static FSharpType GetAbbreviatedType([NotNull] FSharpType fsType)
    {
      while (fsType.IsAbbreviation)
        fsType = fsType.AbbreviatedType;

      return fsType;
    }

    public static bool IsUnit([NotNull] this IType type, [NotNull] IPsiModule psiModule)
    {
      return type.Equals(TypeFactory.CreateTypeByCLRName(UnitClrName, psiModule));
    }

    public static ParameterKind GetParameterKind([NotNull] FSharpParameter param)
    {
      var fsType = param.Type;
      if (fsType.HasTypeDefinition && fsType.TypeDefinition.IsByRef)
      {
        return param.Attributes.Any(a =>
          a.AttributeType.QualifiedName.SubstringBefore(",", StringComparison.Ordinal)
            .Equals("System.Runtime.InteropServices.OutAttribute", StringComparison.Ordinal))
          ? ParameterKind.OUTPUT
          : ParameterKind.REFERENCE;
      }

      return ParameterKind.VALUE;
    }

    public static bool IsParamArray([NotNull] FSharpParameter param)
    {
      return param.Attributes.Any(a =>
        a.AttributeType.QualifiedName.SubstringBefore(",", StringComparison.Ordinal)
          .Equals("System.ParamArrayAttribute", StringComparison.Ordinal));
    }

    private static bool IsNativePtr([NotNull] this FSharpEntity entity)
    {
      return entity.IsFSharp &&
             entity.LogicalName == NativeptrLogicalName &&
             entity.AccessPath == FSharpCoreNamespace;
    }
  }
}