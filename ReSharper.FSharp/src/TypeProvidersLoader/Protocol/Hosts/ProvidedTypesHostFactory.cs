using System;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
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
    private readonly IProvidedRdModelsCreator<ProvidedFieldInfo, RdProvidedFieldInfo> myProvidedFieldInfosCreator;
    private readonly IProvidedRdModelsCreator<ProvidedAssembly, RdProvidedAssembly> myProvidedAssembliesCreator;

    private readonly IReadProvidedCache<Tuple<ProvidedType, RdProvidedType, int>> myProvidedTypesCache;
    private readonly IReadProvidedCache<ITypeProvider> myTypeProvidersCache;

    public ProvidedTypesHostFactory(
      IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo> providedParameterInfosCreator,
      IProvidedRdModelsCreator<ProvidedMethodInfo, RdProvidedMethodInfo> providedMethodInfosCreator,
      IProvidedRdModelsCreator<ProvidedPropertyInfo, RdProvidedPropertyInfo> providedPropertiesCreator,
      IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedTypesCreator,
      IProvidedRdModelsCreator<ProvidedFieldInfo, RdProvidedFieldInfo> providedFieldInfosCreator,
      IProvidedRdModelsCreator<ProvidedAssembly, RdProvidedAssembly> providedAssembliesCreator,
      IReadProvidedCache<Tuple<ProvidedType, RdProvidedType, int>> providedTypesCache,
      IReadProvidedCache<ITypeProvider> typeProvidersCache)
    {
      myProvidedParameterInfosCreator = providedParameterInfosCreator;
      myProvidedMethodInfosCreator = providedMethodInfosCreator;
      myProvidedPropertiesCreator = providedPropertiesCreator;
      myProvidedTypesCreator = providedTypesCreator;
      myProvidedFieldInfosCreator = providedFieldInfosCreator;
      myProvidedAssembliesCreator = providedAssembliesCreator;
      myProvidedTypesCache = providedTypesCache;
      myTypeProvidersCache = typeProvidersCache;
    }

    public void Initialize(RdProvidedTypeProcessModel processModel)
    {
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
      processModel.Assembly.Set(GetAssembly);
      processModel.MakeArrayType.Set(MakeArrayType);
      processModel.MakePointerType.Set(MakePointerType);
      processModel.MakeByRefType.Set(MakeByRefType);
      processModel.GetFields.Set(GetFields);
      processModel.GetField.Set(GetField);
    }

    private RdTask<RdProvidedFieldInfo> GetField(Lifetime lifetime, GetFieldArgs args)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(args.Id);
      var field = myProvidedFieldInfosCreator.CreateRdModel(providedType.GetField(args.FieldName), typeProviderId);
      return RdTask<RdProvidedFieldInfo>.Successful(field);
    }

    private RdTask<RdProvidedFieldInfo[]> GetFields(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var fields = providedType
        .GetFields()
        .Select(t => myProvidedFieldInfosCreator.CreateRdModel(t, typeProviderId))
        .ToArray();
      return RdTask<RdProvidedFieldInfo[]>.Successful(fields);
    }

    private RdTask<int> MakeByRefType(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var byRefTypeId = myProvidedTypesCreator.CreateRdModel(providedType.MakeByRefType(), typeProviderId).EntityId;
      return RdTask<int>.Successful(byRefTypeId);
    }

    private RdTask<int> MakePointerType(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var pointerTypeId = myProvidedTypesCreator.CreateRdModel(providedType.MakePointerType(), typeProviderId).EntityId;
      return RdTask<int>.Successful(pointerTypeId);
    }

    private RdTask<int> MakeArrayType(Lifetime lifetime, MakeArrayTypeArgs args)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(args.Id);
      var arrayTypeId = myProvidedTypesCreator.CreateRdModel(
        args.Rank == 1 ? providedType.MakeArrayType() : providedType.MakeArrayType(args.Rank), typeProviderId).EntityId;
      return RdTask<int>.Successful(arrayTypeId);
    }

    private RdTask<RdProvidedAssembly> GetAssembly(Lifetime lifetime, int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var assembly = myProvidedAssembliesCreator.CreateRdModel(providedType.Assembly, typeProviderId);
      return RdTask<RdProvidedAssembly>.Successful(assembly);
    }

    private RdTask<int> ApplyStaticArguments(Lifetime lifetime, ApplyStaticArgumentsParameters args)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(args.Id);
      var typeProvider = myTypeProvidersCache.Get(typeProviderId);

      var staticArgDescriptions = args.StaticArguments.Select(t => t.Unbox()).ToArray();

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
