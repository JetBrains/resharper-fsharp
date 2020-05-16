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
    private readonly IProvidedRdModelsCreator<ProvidedEventInfo, RdProvidedEventInfo> myProvidedEventInfosCreator;
    private readonly IProvidedRdModelsCreator<ProvidedAssembly, RdProvidedAssembly> myProvidedAssembliesCreator;
    private readonly IProvidedRdModelsCreator<ProvidedVar, RdProvidedVar> myProvidedVarsCreator;

    private readonly IProvidedRdModelsCreator<ProvidedConstructorInfo, RdProvidedConstructorInfo>
      myProvidedConstructorInfosCreator;

    private readonly IReadProvidedCache<Tuple<ProvidedType, RdProvidedType, int>> myProvidedTypesCache;
    private readonly IReadProvidedCache<ITypeProvider> myTypeProvidersCache;

    public ProvidedTypesHostFactory(
      IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo> providedParameterInfosCreator,
      IProvidedRdModelsCreator<ProvidedMethodInfo, RdProvidedMethodInfo> providedMethodInfosCreator,
      IProvidedRdModelsCreator<ProvidedPropertyInfo, RdProvidedPropertyInfo> providedPropertiesCreator,
      IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedTypesCreator,
      IProvidedRdModelsCreator<ProvidedFieldInfo, RdProvidedFieldInfo> providedFieldInfosCreator,
      IProvidedRdModelsCreator<ProvidedEventInfo, RdProvidedEventInfo> providedEventInfosCreator,
      IProvidedRdModelsCreator<ProvidedAssembly, RdProvidedAssembly> providedAssembliesCreator,
      IProvidedRdModelsCreator<ProvidedVar, RdProvidedVar> providedVarsCreator,
      IProvidedRdModelsCreator<ProvidedConstructorInfo, RdProvidedConstructorInfo> providedConstructorInfosCreator,
      IReadProvidedCache<Tuple<ProvidedType, RdProvidedType, int>> providedTypesCache,
      IReadProvidedCache<ITypeProvider> typeProvidersCache)
    {
      myProvidedParameterInfosCreator = providedParameterInfosCreator;
      myProvidedMethodInfosCreator = providedMethodInfosCreator;
      myProvidedPropertiesCreator = providedPropertiesCreator;
      myProvidedTypesCreator = providedTypesCreator;
      myProvidedFieldInfosCreator = providedFieldInfosCreator;
      myProvidedEventInfosCreator = providedEventInfosCreator;
      myProvidedAssembliesCreator = providedAssembliesCreator;
      myProvidedVarsCreator = providedVarsCreator;
      myProvidedConstructorInfosCreator = providedConstructorInfosCreator;
      myProvidedTypesCache = providedTypesCache;
      myTypeProvidersCache = typeProvidersCache;
    }

    public void Initialize(RdProvidedTypeProcessModel processModel)
    {
      processModel.BaseType.Set(GetBaseType);
      processModel.DeclaringType.Set(GetDeclaringType);
      processModel.GetInterfaces.Set(GetInterfaces);
      processModel.GetNestedTypes.Set(GetNestedTypes);
      processModel.GetAllNestedTypes.Set(GetAllNestedTypes);
      processModel.GetGenericTypeDefinition.Set(GetGenericTypeDefinition);
      processModel.GetElementType.Set(GetElementType);
      processModel.GetGenericArguments.Set(GetGenericArguments);
      processModel.GetArrayRank.Set(GetArrayRank);
      processModel.GetEnumUnderlyingType.Set(GetEnumUnderlyingType);
      processModel.GetProperties.Set(GetProperties);
      processModel.GenericParameterPosition.Set(GetGenericParameterPosition);
      processModel.GetStaticParameters.Set(GetStaticParameters);
      processModel.GetMethods.Set(GetMethods);
      processModel.ApplyStaticArguments.Set(ApplyStaticArguments);
      processModel.Assembly.Set(GetAssembly);
      processModel.MakeArrayType.Set(MakeArrayType);
      processModel.MakePointerType.Set(MakePointerType);
      processModel.MakeByRefType.Set(MakeByRefType);
      processModel.GetFields.Set(GetFields);
      processModel.GetEvents.Set(GetEvents);
      processModel.GetConstructors.Set(GetConstructors);
      processModel.AsProvidedVar.Set(AsProvidedVar);
      processModel.MakeGenericType.Set(MakeGenericType);
    }

    private int MakeGenericType(MakeGenericTypeArgs args)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(args.EntityId);
      var providedArgs = args.ArgIds.Select(id => myProvidedTypesCache.Get(id).Item1).ToArray();
      return myProvidedTypesCreator.CreateRdModel(providedType.MakeGenericType(providedArgs), typeProviderId).EntityId;
    }

    private RdProvidedVar AsProvidedVar(AsProvidedVarArgs args)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(args.EntityId);
      return myProvidedVarsCreator.CreateRdModel(providedType.AsProvidedVar(args.Name), typeProviderId);
    }

    private RdProvidedConstructorInfo[] GetConstructors(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return providedType
        .GetConstructors()
        .CreateRdModels(myProvidedConstructorInfosCreator, typeProviderId);
    }

    private RdProvidedEventInfo[] GetEvents(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return providedType
        .GetEvents()
        .CreateRdModels(myProvidedEventInfosCreator, typeProviderId);
    }

    private RdProvidedFieldInfo[] GetFields(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return providedType
        .GetFields()
        .CreateRdModels(myProvidedFieldInfosCreator, typeProviderId);
    }

    private int MakeByRefType(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return myProvidedTypesCreator.CreateRdModel(providedType.MakeByRefType(), typeProviderId).EntityId;
    }

    private int MakePointerType(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return myProvidedTypesCreator.CreateRdModel(providedType.MakePointerType(), typeProviderId).EntityId;
    }

    private int MakeArrayType(MakeArrayTypeArgs args)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(args.Id);
      return myProvidedTypesCreator
        .CreateRdModel(args.Rank == 1 ? providedType.MakeArrayType() : providedType.MakeArrayType(args.Rank),
          typeProviderId).EntityId;
    }

    private RdProvidedAssembly GetAssembly(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return myProvidedAssembliesCreator.CreateRdModel(providedType.Assembly, typeProviderId);
    }

    private int ApplyStaticArguments(ApplyStaticArgumentsParameters args)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(args.Id);
      var typeProvider = myTypeProvidersCache.Get(typeProviderId);

      var staticArgDescriptions = args.StaticArguments.Select(t => t.Unbox()).ToArray();

      return myProvidedTypesCreator
        .CreateRdModel(
          providedType.ApplyStaticArguments(typeProvider, args.TypePathWithArguments, staticArgDescriptions),
          typeProviderId).EntityId;
    }

    private int? GetDeclaringType(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return myProvidedTypesCreator.CreateRdModel(providedType.DeclaringType, typeProviderId)?.EntityId;
    }

    private int? GetBaseType(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return myProvidedTypesCreator.CreateRdModel(providedType.BaseType, typeProviderId)?.EntityId;
    }

    private RdProvidedMethodInfo[] GetMethods(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return providedType
        .GetMethods()
        .CreateRdModels(myProvidedMethodInfosCreator, typeProviderId);
    }

    private RdProvidedParameterInfo[] GetStaticParameters(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      var typeProvider = myTypeProvidersCache.Get(typeProviderId);
      return providedType
        .GetStaticParameters(typeProvider)
        .CreateRdModels(myProvidedParameterInfosCreator, typeProviderId);
    }

    private int GetGenericParameterPosition(int entityId)
    {
      var (providedType, _, _) = myProvidedTypesCache.Get(entityId);
      return providedType.GenericParameterPosition;
    }

    private RdProvidedPropertyInfo[] GetProperties(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return providedType
        .GetProperties()
        .CreateRdModels(myProvidedPropertiesCreator, typeProviderId);
    }

    private int? GetEnumUnderlyingType(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return myProvidedTypesCreator.CreateRdModel(providedType.GetEnumUnderlyingType(), typeProviderId)?.EntityId;
    }

    private int GetArrayRank(int entityId)
    {
      var (providedType, _, _) = myProvidedTypesCache.Get(entityId);
      return providedType.GetArrayRank();
    }

    private int[] GetGenericArguments(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return providedType
        .GetGenericArguments()
        .CreateRdModelsAndReturnIds(myProvidedTypesCreator, typeProviderId);
    }

    private int GetElementType(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return myProvidedTypesCreator.CreateRdModel(providedType.GetElementType(), typeProviderId).EntityId;
    }

    private int GetGenericTypeDefinition(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return myProvidedTypesCreator.CreateRdModel(providedType.GetGenericTypeDefinition(), typeProviderId).EntityId;
    }

    private int[] GetAllNestedTypes(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return providedType
        .GetAllNestedTypes()
        .CreateRdModelsAndReturnIds(myProvidedTypesCreator, typeProviderId);
    }

    private int[] GetNestedTypes(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return providedType
        .GetNestedTypes()
        .CreateRdModelsAndReturnIds(myProvidedTypesCreator, typeProviderId);
    }

    private int[] GetInterfaces(int entityId)
    {
      var (providedType, _, typeProviderId) = myProvidedTypesCache.Get(entityId);
      return providedType
        .GetInterfaces()
        .CreateRdModelsAndReturnIds(myProvidedTypesCreator, typeProviderId);
    }
  }
}
