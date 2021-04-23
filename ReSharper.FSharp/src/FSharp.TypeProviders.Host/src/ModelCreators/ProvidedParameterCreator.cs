using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators
{
  public class ProvidedParameterCreator : ProvidedRdModelsCreatorBase<ProvidedParameterInfo, RdProvidedParameterInfo>
  {
    private readonly TypeProvidersContext myTypeProvidersContext;

    public ProvidedParameterCreator(TypeProvidersContext typeProvidersContext) =>
      myTypeProvidersContext = typeProvidersContext;

    protected override RdProvidedParameterInfo CreateRdModelInternal(ProvidedParameterInfo providedModel,
      int typeProviderId)
    {
      var parameterTypeId =
        myTypeProvidersContext.ProvidedTypeRdModelsCreator.GetOrCreateId(providedModel.ParameterType, typeProviderId);

      var typeProvider = myTypeProvidersContext.TypeProvidersCache.Get(typeProviderId);
      var customAttributes = myTypeProvidersContext.Logger.Catch(() =>
        providedModel
          .GetCustomAttributes(typeProvider)
          .CreateRdModels(myTypeProvidersContext.ProvidedCustomAttributeRdModelsCreator, typeProviderId));

      return new RdProvidedParameterInfo(providedModel.Name, providedModel.IsIn, providedModel.IsOut,
        providedModel.IsOptional, parameterTypeId,
        PrimitiveTypesBoxer.BoxToClientStaticArg(providedModel.RawDefaultValue),
        providedModel.HasDefaultValue, customAttributes);
    }
  }
}
