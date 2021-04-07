using System.Linq;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Hosts
{
  internal class ProvidedTypesHost : IOutOfProcessHost<RdProvidedTypeProcessModel>
  {
    private readonly TypeProvidersContext myTypeProvidersContext;

    public ProvidedTypesHost(TypeProvidersContext typeProvidersContext) =>
      myTypeProvidersContext = typeProvidersContext;

    public void Initialize(RdProvidedTypeProcessModel processModel)
    {
      processModel.GetInterfaces.Set(GetInterfaces);
      processModel.GetAllNestedTypes.Set(GetAllNestedTypes);
      processModel.GetGenericTypeDefinition.Set(GetGenericTypeDefinition);
      processModel.GetElementType.Set(GetElementType);
      processModel.GetArrayRank.Set(GetArrayRank);
      processModel.GetEnumUnderlyingType.Set(GetEnumUnderlyingType);
      processModel.GenericParameterPosition.Set(GetGenericParameterPosition);
      processModel.GetStaticParameters.Set(GetStaticParameters);
      processModel.ApplyStaticArguments.Set(ApplyStaticArguments);
      processModel.GetProvidedType.Set(GetProvidedType);
      processModel.MakeArrayType.Set(MakeArrayType);
      processModel.MakePointerType.Set(MakePointerType);
      processModel.MakeByRefType.Set(MakeByRefType);
      processModel.GetFields.Set(GetFields);
      processModel.MakeGenericType.Set(MakeGenericType);
      processModel.GetProvidedTypes.Set(GetProvidedTypes);
      processModel.GetConstructors.Set(GetConstructors);
      processModel.GetMethods.Set(GetMethods);
      processModel.GetProperties.Set(GetProperties);
      processModel.GetEvents.Set(GetEvents);
    }

    private RdProvidedEventInfo[] GetEvents(int typeId)
    {
      var (type, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(typeId);
      return type
        .GetEvents()
        .CreateRdModels(myTypeProvidersContext.ProvidedEventRdModelsCreator, typeProviderId);
    }

    private RdProvidedPropertyInfo[] GetProperties(int typeId)
    {
      var (type, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(typeId);
      return type
        .GetProperties()
        .CreateRdModels(myTypeProvidersContext.ProvidedPropertyRdModelsCreator, typeProviderId);
    }

    private RdProvidedMethodInfo[] GetMethods(int typeId)
    {
      var (type, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(typeId);
      return type
        .GetMethods()
        .CreateRdModels(myTypeProvidersContext.ProvidedMethodRdModelsCreator, typeProviderId);
    }

    private RdProvidedConstructorInfo[] GetConstructors(int typeId)
    {
      var (type, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(typeId);
      return type
        .GetConstructors()
        .CreateRdModels(myTypeProvidersContext.ProvidedConstructorRdModelsCreator, typeProviderId);
    }

    private RdOutOfProcessProvidedType[] GetProvidedTypes(int[] typeIds)
    {
      var (_, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(typeIds.First());
      return typeIds
        .Select(id => myTypeProvidersContext.ProvidedTypesCache.Get(id).model)
        .CreateRdModels(myTypeProvidersContext.ProvidedTypeRdModelsCreator, typeProviderId);
    }

    private RdOutOfProcessProvidedType GetProvidedType(int entityId)
    {
      var (type, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(entityId);
      return myTypeProvidersContext.ProvidedTypeRdModelsCreator.CreateRdModel(type, typeProviderId);
    }

    private int MakeGenericType(MakeGenericTypeArgs args)
    {
      var (providedType, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(args.EntityId);
      var providedArgs = args.ArgIds
        .Select(argId => myTypeProvidersContext.ProvidedTypesCache.Get(argId).model)
        .ToArray();

      return myTypeProvidersContext.ProvidedTypeRdModelsCreator
        .GetOrCreateId(providedType.MakeGenericType(providedArgs), typeProviderId);
    }

    private RdProvidedFieldInfo[] GetFields(int entityId)
    {
      var (providedType, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(entityId);
      return providedType
        .GetFields()
        .CreateRdModels(myTypeProvidersContext.ProvidedFieldRdModelsCreator, typeProviderId);
    }

    private int MakeByRefType(int entityId)
    {
      var (providedType, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(entityId);
      return myTypeProvidersContext.ProvidedTypeRdModelsCreator.GetOrCreateId(providedType.MakeByRefType(),
        typeProviderId);
    }

    private int MakePointerType(int entityId)
    {
      var (providedType, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(entityId);
      return myTypeProvidersContext.ProvidedTypeRdModelsCreator.GetOrCreateId(providedType.MakePointerType(),
        typeProviderId);
    }

    private int MakeArrayType(MakeArrayTypeArgs args)
    {
      var (providedType, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(args.Id);
      return myTypeProvidersContext.ProvidedTypeRdModelsCreator
        .GetOrCreateId(args.Rank == 1 ? providedType.MakeArrayType() : providedType.MakeArrayType(args.Rank),
          typeProviderId);
    }

    private int ApplyStaticArguments(ApplyStaticArgumentsParameters args)
    {
      var (providedType, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(args.Id);
      var typeProvider = myTypeProvidersContext.TypeProvidersCache.Get(typeProviderId);

      var staticArgDescriptions = args.StaticArguments.Unbox();

      return myTypeProvidersContext.ProvidedTypeRdModelsCreator
        .GetOrCreateId(
          providedType.ApplyStaticArguments(typeProvider, args.TypePathWithArguments, staticArgDescriptions),
          typeProviderId);
    }

    private RdProvidedParameterInfo[] GetStaticParameters(int entityId)
    {
      var (providedType, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(entityId);
      var typeProvider = myTypeProvidersContext.TypeProvidersCache.Get(typeProviderId);
      return providedType
        .GetStaticParameters(typeProvider)
        .CreateRdModels(myTypeProvidersContext.ProvidedParameterRdModelsCreator, typeProviderId);
    }

    private int GetGenericParameterPosition(int entityId)
    {
      var (providedType, _) = myTypeProvidersContext.ProvidedTypesCache.Get(entityId);
      return providedType.GenericParameterPosition;
    }

    private int GetEnumUnderlyingType(int entityId)
    {
      var (providedType, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(entityId);
      return myTypeProvidersContext.ProvidedTypeRdModelsCreator
        .GetOrCreateId(providedType.GetEnumUnderlyingType(), typeProviderId);
    }

    private int GetArrayRank(int entityId)
    {
      var (providedType, _) = myTypeProvidersContext.ProvidedTypesCache.Get(entityId);
      return providedType.GetArrayRank();
    }

    private int GetElementType(int entityId)
    {
      var (providedType, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(entityId);
      return myTypeProvidersContext.ProvidedTypeRdModelsCreator.GetOrCreateId(providedType.GetElementType(),
        typeProviderId);
    }

    private int GetGenericTypeDefinition(int entityId)
    {
      var (providedType, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(entityId);
      return myTypeProvidersContext.ProvidedTypeRdModelsCreator.GetOrCreateId(providedType.GetGenericTypeDefinition(),
        typeProviderId);
    }

    private int[] GetAllNestedTypes(int entityId)
    {
      var (providedType, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(entityId);
      return providedType
        .GetAllNestedTypes()
        .CreateIds(myTypeProvidersContext.ProvidedTypeRdModelsCreator, typeProviderId);
    }

    private int[] GetInterfaces(int entityId)
    {
      var (providedType, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(entityId);
      return providedType
        .GetInterfaces()
        .CreateIds(myTypeProvidersContext.ProvidedTypeRdModelsCreator, typeProviderId);
    }
  }
}
