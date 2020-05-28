using System;
using System.Linq;
using JetBrains.Core;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class TypeProvidersLoaderHostFactory : IOutOfProcessHostFactory<RdFSharpTypeProvidersLoaderModel>
  {
    private readonly UnitOfWork myUnitOfWork;

    public TypeProvidersLoaderHostFactory(UnitOfWork unitOfWork)
    {
      myUnitOfWork = unitOfWork;
    }

    public void Initialize(RdFSharpTypeProvidersLoaderModel model)
    {
      model.InstantiateTypeProvidersOfAssembly.Set((lifetime, args) =>
        InstantiateTypeProvidersOfAssembly(args, model.RdTypeProviderProcessModel));

      model.Kill.Set(Die);
    }

    private static Unit Die(Unit _)
    {
      Environment.Exit(0);
      return Unit.Instance;
    }

    private RdTask<RdTypeProvider[]> InstantiateTypeProvidersOfAssembly(
      InstantiateTypeProvidersOfAssemblyParameters @params, RdTypeProviderProcessModel processModel)
    {
      var instantiateResults = myUnitOfWork.TypeProvidersLoader.InstantiateTypeProvidersOfAssembly(@params)
        .Select(t =>
        {
          var typeProviderRdModel = myUnitOfWork.TypeProviderRdModelsCreator.CreateRdModel(t, -1);
          t.Invalidate +=
            (obj, args) =>
            {
              InvalidateCaches(typeProviderRdModel.EntityId);
              processModel.Invalidate.Fire(typeProviderRdModel.EntityId);
            };
          return typeProviderRdModel;
        })
        .ToArray();
      return RdTask<RdTypeProvider[]>.Successful(instantiateResults);
    }

    //TODO: Task.Run?
    private void InvalidateCaches(int typeProviderId)
    {
      myUnitOfWork.ProvidedVarsCache.RemoveAll(typeProviderId);
      myUnitOfWork.ProvidedExprsCache.RemoveAll(typeProviderId);
      myUnitOfWork.ProvidedConstructorInfosCache.RemoveAll(typeProviderId);
      myUnitOfWork.ProvidedFieldInfosCache.RemoveAll(typeProviderId);
      myUnitOfWork.ProvidedAssembliesCache.RemoveAll(typeProviderId);
      myUnitOfWork.ProvidedMethodInfosCache.RemoveAll(typeProviderId);
      myUnitOfWork.ProvidedPropertyInfosCache.RemoveAll(typeProviderId);
      myUnitOfWork.ProvidedParameterInfosCache.RemoveAll(typeProviderId);
      myUnitOfWork.ProvidedTypesCache.RemoveAll(typeProviderId);
      myUnitOfWork.ProvidedNamespacesCache.RemoveAll(typeProviderId);
      myUnitOfWork.ProvidedEventInfosCache.RemoveAll(typeProviderId);
    }
  }
}
