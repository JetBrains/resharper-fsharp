using System.Linq;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  internal class ProvidedMethodInfosHost : IOutOfProcessHost<RdProvidedMethodInfoProcessModel>
  {
    private readonly TypeProvidersContext myTypeProvidersContext;

    public ProvidedMethodInfosHost(TypeProvidersContext typeProvidersContext) =>
      myTypeProvidersContext = typeProvidersContext;

    public void Initialize(RdProvidedMethodInfoProcessModel processModel)
    {
      processModel.GetParameters.Set(GetParameters);
      processModel.GetStaticParametersForMethod.Set(GetStaticParametersForMethod);
      processModel.ApplyStaticArgumentsForMethod.Set(ApplyStaticArgumentsForMethod);
      processModel.GetProvidedMethodInfo.Set(GetProvidedMethodInfo);
      processModel.GetProvidedMethodInfos.Set(GetProvidedMethodInfos);
    }

    private RdProvidedMethodInfo[] GetProvidedMethodInfos(int[] methodIds)
    {
      var (_, typeProviderId) = myTypeProvidersContext.ProvidedMethodsCache.Get(methodIds.First());
      return methodIds
        .Select(id => myTypeProvidersContext.ProvidedMethodsCache.Get(id).model)
        .CreateRdModels(myTypeProvidersContext.ProvidedMethodRdModelsCreator, typeProviderId);
    }

    private RdProvidedMethodInfo GetProvidedMethodInfo(int entityId)
    {
      var (providedMethod, typeProviderId) = myTypeProvidersContext.ProvidedMethodsCache.Get(entityId);
      return myTypeProvidersContext.ProvidedMethodRdModelsCreator.CreateRdModel(providedMethod, typeProviderId);
    }

    private RdProvidedMethodInfo ApplyStaticArgumentsForMethod(ApplyStaticArgumentsForMethodArgs args)
    {
      var (providedMethod, typeProviderId) = myTypeProvidersContext.ProvidedMethodsCache.Get(args.EntityId);
      var typeProvider = myTypeProvidersContext.TypeProvidersCache.Get(typeProviderId);
      var createdMethod = providedMethod.ApplyStaticArgumentsForMethod(typeProvider, args.FullNameAfterArguments,
        args.StaticArgs.Unbox()) as ProvidedMethodInfo;

      return myTypeProvidersContext.ProvidedMethodRdModelsCreator.CreateRdModel(createdMethod, typeProviderId);
    }

    private RdProvidedParameterInfo[] GetStaticParametersForMethod(int entityId)
    {
      var (providedMethod, typeProviderId) = myTypeProvidersContext.ProvidedMethodsCache.Get(entityId);
      var typeProvider = myTypeProvidersContext.TypeProvidersCache.Get(typeProviderId);
      return providedMethod
        .GetStaticParametersForMethod(typeProvider)
        .CreateRdModels(myTypeProvidersContext.ProvidedParameterRdModelsCreator, typeProviderId);
    }

    private RdProvidedParameterInfo[] GetParameters(int entityId)
    {
      var (providedMethod, typeProviderId) = myTypeProvidersContext.ProvidedMethodsCache.Get(entityId);
      return providedMethod
        .GetParameters()
        .CreateRdModels(myTypeProvidersContext.ProvidedParameterRdModelsCreator, typeProviderId);
    }
  }
}
