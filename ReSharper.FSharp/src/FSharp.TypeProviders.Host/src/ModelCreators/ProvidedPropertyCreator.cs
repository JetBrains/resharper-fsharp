using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators
{
  public class
    ProvidedPropertyCreator : ProvidedRdModelsCreatorWithCacheBase<ProvidedPropertyInfo, RdProvidedPropertyInfo>
  {
    private readonly TypeProvidersContext myTypeProvidersContext;

    public ProvidedPropertyCreator(TypeProvidersContext typeProvidersContext) :
      base(typeProvidersContext.ProvidedPropertyCache) =>
      myTypeProvidersContext = typeProvidersContext;

    protected override RdProvidedPropertyInfo CreateRdModelInternal(ProvidedPropertyInfo providedModel,
      int entityId, int typeProviderId)
    {
      var declaringTypeId = myTypeProvidersContext.ProvidedTypeRdModelsCreator
        .GetOrCreateId(providedModel.DeclaringType, typeProviderId);

      var propertyTypeId = myTypeProvidersContext.ProvidedTypeRdModelsCreator
        .GetOrCreateId(providedModel.PropertyType, typeProviderId);

      var getMethod = myTypeProvidersContext.ProvidedMethodRdModelsCreator
        .GetOrCreateId(providedModel.GetGetMethod(), typeProviderId);

      var setMethod = myTypeProvidersContext.ProvidedMethodRdModelsCreator
        .GetOrCreateId(providedModel.GetSetMethod(), typeProviderId);

      var indexParameters = providedModel
        .GetIndexParameters()
        .CreateRdModels(myTypeProvidersContext.ProvidedParameterRdModelsCreator, typeProviderId);

      return new RdProvidedPropertyInfo(declaringTypeId, propertyTypeId, getMethod, setMethod,
        providedModel.CanRead, providedModel.CanWrite, indexParameters, providedModel.Name, entityId);
    }
  }
}
