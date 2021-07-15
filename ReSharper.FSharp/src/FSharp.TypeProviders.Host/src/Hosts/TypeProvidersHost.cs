using System;
using System.Collections.Generic;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;
using static FSharp.Compiler.ExtensionTyping;
using Unit = JetBrains.Core.Unit;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Hosts
{
  internal class TypeProvidersHost : IOutOfProcessHost<RdTypeProviderProcessModel>
  {
    private readonly TypeProvidersContext myTypeProvidersContext;

    public TypeProvidersHost(TypeProvidersContext typeProvidersContext) =>
      myTypeProvidersContext = typeProvidersContext;

    public void Initialize(RdTypeProviderProcessModel processModel)
    {
      processModel.GetProvidedNamespaces.Set(GetProvidedNamespaces);
      processModel.GetCustomAttributes.Set(GetCustomAttributes);
      processModel.Dispose.Set(Dispose);
      processModel.InstantiateTypeProvidersOfAssembly.Set(
        args => InstantiateTypeProvidersOfAssembly(args, processModel));
      processModel.Kill.Set(Die);
    }

    private RdCustomAttributeData[] GetCustomAttributes(GetCustomAttributesArgs args)
    {
      int typeProviderId;
      IProvidedCustomAttributeProvider provider;
      switch (args.ProvidedEntityType)
      {
        case RdProvidedEntityType.TypeInfo:
          (provider, typeProviderId) = myTypeProvidersContext.ProvidedTypesCache.Get(args.EntityId);
          break;
        case RdProvidedEntityType.MethodInfo:
          (provider, typeProviderId) = myTypeProvidersContext.ProvidedMethodsCache.Get(args.EntityId);
          break;
        case RdProvidedEntityType.ConstructorInfo:
          (provider, typeProviderId) = myTypeProvidersContext.ProvidedConstructorsCache.Get(args.EntityId);
          break;
        case RdProvidedEntityType.PropertyInfo:
          (provider, typeProviderId) = myTypeProvidersContext.ProvidedPropertyCache.Get(args.EntityId);
          break;
        default:
          throw new InvalidOperationException($"Unexpected EntityType {args.ProvidedEntityType}");
      }

      var typeProvider = myTypeProvidersContext.TypeProvidersCache.Get(typeProviderId);
      return provider
        .GetCustomAttributes(typeProvider)
        .CreateRdModels(myTypeProvidersContext.ProvidedCustomAttributeRdModelsCreator, typeProviderId);
    }

    private RdProvidedNamespace[] GetProvidedNamespaces(int entityId)
    {
      var typeProvider = myTypeProvidersContext.TypeProvidersCache.Get(entityId);
      return typeProvider
        .GetNamespaces()
        .CreateRdModels(myTypeProvidersContext.ProvidedNamespaceRdModelsCreator, entityId);
    }

    private Unit Dispose(int typeProviderId)
    {
      if (myTypeProvidersContext.TypeProvidersCache.Contains(typeProviderId))
      {
        myTypeProvidersContext.ProvidedConstructorsCache.Remove(typeProviderId);
        myTypeProvidersContext.ProvidedMethodsCache.Remove(typeProviderId);
        myTypeProvidersContext.ProvidedPropertyCache.Remove(typeProviderId);
        myTypeProvidersContext.ProvidedAssembliesCache.Remove(typeProviderId);
        myTypeProvidersContext.ProvidedTypesCache.Remove(typeProviderId);
        myTypeProvidersContext.TypeProvidersCache.Remove(typeProviderId);
      }

      return Unit.Instance;
    }

    private InstantiationResult InstantiateTypeProvidersOfAssembly(InstantiateTypeProvidersOfAssemblyParameters @params,
      RdTypeProviderProcessModel processModel)
    {
      var runtimeAssembly = @params.RunTimeAssemblyFileName;
      var environment = @params.RdResolutionEnvironment.ResolutionFolder;
      var envKey = $"{runtimeAssembly}+{environment}";

      var typeProviders = myTypeProvidersContext.TypeProvidersLoader.InstantiateTypeProvidersOfAssembly(@params);
      var rdTypeProviders = new List<RdTypeProvider>();
      var cachedIds = new List<int>();

      foreach (var typeProvider in typeProviders)
      {
        if (myTypeProvidersContext.TypeProvidersCache.TryGetInfo(typeProvider, envKey, out var info))
        {
          if (info.isInvalidated) Dispose(info.key);
          else
          {
            cachedIds.Add(info.key);
            continue;
          }
        }

        var typeProviderRdModel =
          myTypeProvidersContext.TypeProviderRdModelsCreator.CreateRdModel(typeProvider, envKey);

        typeProvider.Invalidate += (_, __) =>
          processModel.Proto.Scheduler.Queue(() => processModel.Invalidate.Fire(typeProviderRdModel!.EntityId));

        rdTypeProviders.Add(typeProviderRdModel);
      }

      return new InstantiationResult(rdTypeProviders.ToArray(), cachedIds.ToArray());
    }

    private Unit Die(Unit _)
    {
      myTypeProvidersContext.TypeProvidersLoader.Dispose();
      Environment.Exit(0);
      return Unit.Instance;
    }
  }
}
