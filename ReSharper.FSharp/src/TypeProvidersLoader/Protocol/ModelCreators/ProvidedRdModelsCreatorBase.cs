using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public abstract class ProvidedRdModelsCreatorBase<T, Tu> : IProvidedRdModelsCreator<T, Tu> where Tu : class
  {
    [ContractAnnotation("providedModel:null => null")]
    public Tu CreateRdModel(T providedModel, int typeProviderId) =>
      providedModel == null ? null : CreateRdModelInternal(providedModel, typeProviderId);

    protected abstract Tu CreateRdModelInternal(T providedModel, int typeProviderId);
  }
}
