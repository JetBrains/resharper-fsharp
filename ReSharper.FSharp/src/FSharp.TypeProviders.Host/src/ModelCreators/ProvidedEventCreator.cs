using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators
{
  public class ProvidedEventCreator : ProvidedRdModelsCreatorBase<ProvidedEventInfo, RdProvidedEventInfo>
  {
    private readonly TypeProvidersContext myTypeProvidersContext;

    public ProvidedEventCreator(TypeProvidersContext typeProvidersContext) =>
      myTypeProvidersContext = typeProvidersContext;

    protected override RdProvidedEventInfo CreateRdModelInternal(ProvidedEventInfo providedModel, int typeProviderId)
    {
      var declaringTypeId =
        myTypeProvidersContext.ProvidedTypeRdModelsCreator.GetOrCreateId(providedModel.DeclaringType, typeProviderId);

      var eventHandlerType = myTypeProvidersContext.ProvidedTypeRdModelsCreator
        .GetOrCreateId(providedModel.EventHandlerType, typeProviderId);

      var typeProvider = myTypeProvidersContext.TypeProvidersCache.Get(typeProviderId);
      var customAttributes = providedModel
        .GetCustomAttributes(typeProvider)
        .CreateRdModels(myTypeProvidersContext.ProvidedCustomAttributeRdModelsCreator, typeProviderId);

      var addMethod =
        myTypeProvidersContext.ProvidedMethodRdModelsCreator.GetOrCreateId(providedModel.GetAddMethod(),
          typeProviderId);

      var removeMethod =
        myTypeProvidersContext.ProvidedMethodRdModelsCreator.GetOrCreateId(providedModel.GetRemoveMethod(),
          typeProviderId);

      return new RdProvidedEventInfo(providedModel.Name, declaringTypeId, eventHandlerType, addMethod, removeMethod,
        customAttributes);
    }
  }
}
