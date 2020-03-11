using System;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedTypesHostFactory : IOutOfProcessHostFactory<RdProvidedTypeProcessModel>
  {
    private readonly IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo>
      myProvidedParameterInfosCreator;

    private readonly IProvidedRdModelsCreator<ProvidedMethodInfo, RdProvidedMethodInfo>
      myProvidedMethodInfosCreator;

    private readonly IProvidedRdModelsCreator<ProvidedPropertyInfo, RdProvidedPropertyInfo>
      myProvidedPropertiesCreator;

    private readonly IProvidedRdModelsCreator<ProvidedType, RdProvidedType> myProvidedTypesCreator;

    private readonly IReadProvidedCache<Tuple<ProvidedType, RdProvidedType, int>> myProvidedTypesCache;
    private readonly IReadProvidedCache<ITypeProvider> myTypeProvidersCache;

    public ProvidedTypesHostFactory(
      IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo> providedParameterInfosCreator,
      IProvidedRdModelsCreator<ProvidedMethodInfo, RdProvidedMethodInfo> providedMethodInfosCreator,
      IProvidedRdModelsCreator<ProvidedPropertyInfo, RdProvidedPropertyInfo> providedPropertiesCreator,
      IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedTypesCreator,
      IReadProvidedCache<Tuple<ProvidedType, RdProvidedType, int>> providedTypesCache,
      IReadProvidedCache<ITypeProvider> typeProvidersCache)
    {
      myProvidedParameterInfosCreator = providedParameterInfosCreator;
      myProvidedMethodInfosCreator = providedMethodInfosCreator;
      myProvidedPropertiesCreator = providedPropertiesCreator;
      myProvidedTypesCreator = providedTypesCreator;
      myProvidedTypesCache = providedTypesCache;
      myTypeProvidersCache = typeProvidersCache;
    }

    public RdProvidedTypeProcessModel CreateProcessModel()
    {
      var processModel = new RdProvidedTypeProcessModel();
      processModel.BaseType.Set(GetBaseType);
      processModel.DeclaringType.Set(GetDeclaringType);
      processModel.GetInterfaces.Set(GetInterfaces);
      processModel.GetNestedType.Set(GetNestedType);
      processModel.GetNestedTypes.Set(GetNestedTypes);
      processModel.GetAllNestedTypes.Set(GetAllNestedTypes);
      processModel.GetGenericTypeDefinition.Set(GetGenericTypeDefinition);
      processModel.GetElementType.Set(GetElementType);
      processModel.GetGenericArguments.Set(GetGenericArguments);
      processModel.GetArrayRank.Set(GetArrayRank);
      processModel.GetEnumUnderlyingType.Set(GetEnumUnderlyingType);
      processModel.GetProperties.Set(GetProperties);
      processModel.GetProperty.Set(GetProperty);
      processModel.GenericParameterPosition.Set(GetGenericParameterPosition);
      processModel.GetStaticParameters.Set(GetStaticParameters);
      processModel.GetMethods.Set(GetMethods);
      processModel.ApplyStaticArguments.Set(ApplyStaticArguments);

      return processModel;
    }

    private RdTask<int> ApplyStaticArguments(Lifetime lifetime, ApplyStaticArgumentsParameters args)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(args.Id);
      var typeProvider = myTypeProvidersCache.Get(typeProviderId);

      var staticArgDescriptions = args.StaticArguments.Select(t => t.TypeName switch
      {
        "sbyte" => sbyte.Parse(t.Value),
        "short" => short.Parse(t.Value),
        "int" => int.Parse(t.Value),
        "long" => long.Parse(t.Value),
        "byte" => byte.Parse(t.Value),
        "ushort" => ushort.Parse(t.Value),
        "uint" => uint.Parse(t.Value),
        "ulong" => ulong.Parse(t.Value),
        "decimal" => decimal.Parse(t.Value),
        "float" => float.Parse(t.Value),
        "double" => double.Parse(t.Value),
        "char" => char.Parse(t.Value),
        "bool" => bool.Parse(t.Value),
        "string" => (object) t.Value,
        _ => throw new ArgumentException($"Unexpected static arg with type {t.TypeName}")
      }).ToArray();

      var type = myProvidedTypesCreator
        .CreateRdModel(
          providedType.ApplyStaticArguments(typeProvider, args.TypePathWithArguments, staticArgDescriptions),
          typeProviderId).EntityId;
      return RdTask<int>.Successful(type);
    }

    private RdTask<int?> GetDeclaringType(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var declaringTypeId = myProvidedTypesCreator.CreateRdModel(providedType.DeclaringType, typeProviderId)?.EntityId;
      return RdTask<int?>.Successful(declaringTypeId);
    }

    private RdTask<int?> GetBaseType(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var baseType = myProvidedTypesCreator.CreateRdModel(providedType.BaseType, typeProviderId)?.EntityId;
      return RdTask<int?>.Successful(baseType);
    }

    private RdTask<RdProvidedMethodInfo[]> GetMethods(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var interfaces = providedType
        .GetMethods()
        .Select(t => myProvidedMethodInfosCreator.CreateRdModel(t, typeProviderId)).ToArray();
      return RdTask<RdProvidedMethodInfo[]>.Successful(interfaces);
    }

    private RdTask<RdProvidedParameterInfo[]> GetStaticParameters(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var typeProvider = myTypeProvidersCache.Get(typeProviderId);
      var staticParameters = providedType
        .GetStaticParameters(typeProvider)
        .Select(t => myProvidedParameterInfosCreator.CreateRdModel(t, typeProviderId))
        .ToArray();
      return RdTask<RdProvidedParameterInfo[]>.Successful(staticParameters);
    }

    private RdTask<int> GetGenericParameterPosition(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var genericParameterPosition = providedType.GetArrayRank();
      return RdTask<int>.Successful(genericParameterPosition);
    }

    private RdTask<RdProvidedPropertyInfo> GetProperty(Lifetime lifetime, GetPropertyArgs args)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(args.Id);
      var property =
        myProvidedPropertiesCreator.CreateRdModel(providedType.GetProperty(args.PropertyName), typeProviderId);
      return RdTask<RdProvidedPropertyInfo>.Successful(property);
    }

    private RdTask<RdProvidedPropertyInfo[]> GetProperties(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var properties = providedType
        .GetProperties()
        .Select(t => myProvidedPropertiesCreator.CreateRdModel(t, typeProviderId))
        .ToArray();
      return RdTask<RdProvidedPropertyInfo[]>.Successful(properties);
    }

    private RdTask<int?> GetEnumUnderlyingType(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var enumUnderlyingTypeId =
        myProvidedTypesCreator.CreateRdModel(providedType.GetEnumUnderlyingType(), typeProviderId)?.EntityId;
      return RdTask<int?>.Successful(enumUnderlyingTypeId);
    }

    private RdTask<int> GetArrayRank(Lifetime lifetime, int entityId)
    {
      var (providedType, _, _) = myProvidedTypesCache.Get(entityId);
      var arrayRank = providedType.GetArrayRank();
      return RdTask<int>.Successful(arrayRank);
    }

    private RdTask<int[]> GetGenericArguments(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var genericArguments = providedType
        .GetGenericArguments()
        .Select(t => myProvidedTypesCreator.CreateRdModel(t, typeProviderId).EntityId)
        .ToArray();
      return RdTask<int[]>.Successful(genericArguments);
    }

    private RdTask<int> GetElementType(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var elementType = myProvidedTypesCreator.CreateRdModel(providedType.GetElementType(), typeProviderId).EntityId;
      return RdTask<int>.Successful(elementType);
    }

    private RdTask<int> GetGenericTypeDefinition(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var genericTypeDefinition = myProvidedTypesCreator
        .CreateRdModel(providedType.GetGenericTypeDefinition(), typeProviderId).EntityId;
      return RdTask<int>.Successful(genericTypeDefinition);
    }

    private RdTask<int[]> GetAllNestedTypes(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var nestedTypes = providedType
        .GetAllNestedTypes()
        .Select(t => myProvidedTypesCreator.CreateRdModel(t, typeProviderId).EntityId)
        .ToArray();
      return RdTask<int[]>.Successful(nestedTypes);
    }

    private RdTask<int[]> GetNestedTypes(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var nestedTypes = providedType
        .GetNestedTypes()
        .Select(t => myProvidedTypesCreator.CreateRdModel(t, typeProviderId).EntityId)
        .ToArray();
      return RdTask<int[]>.Successful(nestedTypes);
    }

    private RdTask<int> GetNestedType(Lifetime lifetime, GetNestedTypeArgs args)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(args.Id);
      var nestedType = myProvidedTypesCreator.CreateRdModel(providedType.GetNestedType(args.TypeName), typeProviderId)
        .EntityId;
      return RdTask<int>.Successful(nestedType);
    }

    private RdTask<int[]> GetInterfaces(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var interfaces = providedType
        .GetInterfaces()
        .Select(t => myProvidedTypesCreator.CreateRdModel(t, typeProviderId).EntityId)
        .ToArray();
      return RdTask<int[]>.Successful(interfaces);
    }
  }
}
