using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Hosts
{
  internal class ProvidedConstructorInfosHost : IOutOfProcessHost<RdProvidedConstructorInfoProcessModel>
  {
    private readonly TypeProvidersContext myTypeProvidersContext;

    public ProvidedConstructorInfosHost(TypeProvidersContext typeProvidersContext) =>
      myTypeProvidersContext = typeProvidersContext;

    public void Initialize(RdProvidedConstructorInfoProcessModel processModel)
    {
      processModel.GetParameters.Set(GetParameters);
      processModel.GetStaticParametersForMethod.Set(GetStaticParametersForMethod);
      processModel.ApplyStaticArgumentsForMethod.Set(ApplyStaticArgumentsForMethod);
    }

    private RdProvidedConstructorInfo ApplyStaticArgumentsForMethod(ApplyStaticArgumentsForMethodArgs args)
    {
      var (providedConstructor, typeProviderId) = myTypeProvidersContext.ProvidedConstructorsCache.Get(args.EntityId);
      var typeProvider = myTypeProvidersContext.TypeProvidersCache.Get(typeProviderId);
      var createdConstructor =
        providedConstructor.ApplyStaticArgumentsForMethod(typeProvider, args.FullNameAfterArguments,
          args.StaticArgs.Unbox()) as ProvidedConstructorInfo;

      return myTypeProvidersContext.ProvidedConstructorRdModelsCreator.CreateRdModel(createdConstructor,
        typeProviderId);
    }

    private RdProvidedParameterInfo[] GetStaticParametersForMethod(int entityId)
    {
      var (providedConstructor, typeProviderId) = myTypeProvidersContext.ProvidedConstructorsCache.Get(entityId);
      var typeProvider = myTypeProvidersContext.TypeProvidersCache.Get(typeProviderId);
      return providedConstructor
        .GetStaticParametersForMethod(typeProvider)
        .CreateRdModels(myTypeProvidersContext.ProvidedParameterRdModelsCreator, typeProviderId);
    }

    private RdProvidedParameterInfo[] GetParameters(int entityId)
    {
      var (providedConstructor, typeProviderId) = myTypeProvidersContext.ProvidedConstructorsCache.Get(entityId);
      return providedConstructor
        .GetParameters()
        .CreateRdModels(myTypeProvidersContext.ProvidedParameterRdModelsCreator, typeProviderId);
    }
  }
}
