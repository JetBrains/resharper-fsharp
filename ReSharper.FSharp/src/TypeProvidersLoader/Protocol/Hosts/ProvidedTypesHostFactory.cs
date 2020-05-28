using System;
using System.Linq;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedTypesHostFactory : IOutOfProcessHostFactory<RdProvidedTypeProcessModel>
  {
    private readonly UnitOfWork myUnitOfWork;

    public ProvidedTypesHostFactory(UnitOfWork unitOfWork)
    {
      myUnitOfWork = unitOfWork;
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
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(args.EntityId);
      var providedArgs = args.ArgIds.Select(id => myUnitOfWork.ProvidedTypesCache.Get(id).Item1).ToArray();
      return myUnitOfWork.ProvidedTypeRdModelsCreator
        .CreateRdModel(providedType.MakeGenericType(providedArgs), typeProviderId).EntityId;
    }

    private RdProvidedVar AsProvidedVar(AsProvidedVarArgs args)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(args.EntityId);
      return myUnitOfWork.ProvidedVarRdModelsCreator.CreateRdModel(providedType.AsProvidedVar(args.Name),
        typeProviderId);
    }

    private RdProvidedConstructorInfo[] GetConstructors(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return providedType
        .GetConstructors()
        .CreateRdModels(myUnitOfWork.ProvidedConstructorInfoRdModelsCreator, typeProviderId);
    }

    private RdProvidedEventInfo[] GetEvents(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return providedType
        .GetEvents()
        .CreateRdModels(myUnitOfWork.ProvidedEventInfoRdModelsCreator, typeProviderId);
    }

    private RdProvidedFieldInfo[] GetFields(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return providedType
        .GetFields()
        .CreateRdModels(myUnitOfWork.ProvidedFieldInfoRdModelsCreator, typeProviderId);
    }

    private int MakeByRefType(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(providedType.MakeByRefType(), typeProviderId)
        .EntityId;
    }

    private int MakePointerType(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(providedType.MakePointerType(), typeProviderId)
        .EntityId;
    }

    private int MakeArrayType(MakeArrayTypeArgs args)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(args.Id);
      return myUnitOfWork.ProvidedTypeRdModelsCreator
        .CreateRdModel(args.Rank == 1 ? providedType.MakeArrayType() : providedType.MakeArrayType(args.Rank),
          typeProviderId).EntityId;
    }

    private RdProvidedAssembly GetAssembly(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return myUnitOfWork.ProvidedAssemblyRdModelsCreator.CreateRdModel(providedType.Assembly, typeProviderId);
    }

    private int ApplyStaticArguments(ApplyStaticArgumentsParameters args)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(args.Id);
      var typeProvider = myUnitOfWork.TypeProvidersCache.Get(typeProviderId);

      var staticArgDescriptions = args.StaticArguments.Select(t => t.Unbox()).ToArray();

      return myUnitOfWork.ProvidedTypeRdModelsCreator
        .CreateRdModel(
          providedType.ApplyStaticArguments(typeProvider, args.TypePathWithArguments, staticArgDescriptions),
          typeProviderId).EntityId;
    }

    private int? GetDeclaringType(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(providedType.DeclaringType, typeProviderId)
        ?.EntityId;
    }

    private int? GetBaseType(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(providedType.BaseType, typeProviderId)?.EntityId;
    }

    private RdProvidedMethodInfo[] GetMethods(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return providedType
        .GetMethods()
        .CreateRdModels(myUnitOfWork.ProvidedMethodInfoRdModelsCreator, typeProviderId);
    }

    private RdProvidedParameterInfo[] GetStaticParameters(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      var typeProvider = myUnitOfWork.TypeProvidersCache.Get(typeProviderId);
      return providedType
        .GetStaticParameters(typeProvider)
        .CreateRdModels(myUnitOfWork.ProvidedParameterInfoRdModelsCreator, typeProviderId);
    }

    private int GetGenericParameterPosition(int entityId)
    {
      var (providedType, _, _) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return providedType.GenericParameterPosition;
    }

    private RdProvidedPropertyInfo[] GetProperties(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return providedType
        .GetProperties()
        .CreateRdModels(myUnitOfWork.ProvidedPropertyInfoRdModelsCreator, typeProviderId);
    }

    private int? GetEnumUnderlyingType(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator
        .CreateRdModel(providedType.GetEnumUnderlyingType(), typeProviderId)?.EntityId;
    }

    private int GetArrayRank(int entityId)
    {
      var (providedType, _, _) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return providedType.GetArrayRank();
    }

    private int[] GetGenericArguments(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return providedType
        .GetGenericArguments()
        .CreateRdModelsAndReturnIds(myUnitOfWork.ProvidedTypeRdModelsCreator, typeProviderId);
    }

    private int GetElementType(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(providedType.GetElementType(), typeProviderId)
        .EntityId;
    }

    private int GetGenericTypeDefinition(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator
        .CreateRdModel(providedType.GetGenericTypeDefinition(), typeProviderId).EntityId;
    }

    private int[] GetAllNestedTypes(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return providedType
        .GetAllNestedTypes()
        .CreateRdModelsAndReturnIds(myUnitOfWork.ProvidedTypeRdModelsCreator, typeProviderId);
    }

    private int[] GetNestedTypes(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return providedType
        .GetNestedTypes()
        .CreateRdModelsAndReturnIds(myUnitOfWork.ProvidedTypeRdModelsCreator, typeProviderId);
    }

    private int[] GetInterfaces(int entityId)
    {
      var (providedType, _, typeProviderId) = myUnitOfWork.ProvidedTypesCache.Get(entityId);
      return providedType
        .GetInterfaces()
        .CreateRdModelsAndReturnIds(myUnitOfWork.ProvidedTypeRdModelsCreator, typeProviderId);
    }
  }
}
