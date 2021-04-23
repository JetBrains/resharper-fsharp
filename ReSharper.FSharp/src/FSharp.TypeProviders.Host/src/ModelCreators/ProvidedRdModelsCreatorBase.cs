using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators
{
  public abstract class ProvidedRdModelsCreatorBase<T, TU> : IProvidedRdModelsCreator<T, TU> where TU : class
  {
    [ContractAnnotation("providedModel:null => null")]
    public TU CreateRdModel(T providedModel, int typeProviderId) =>
      providedModel == null ? null : CreateRdModelInternal(providedModel, typeProviderId);

    protected abstract TU CreateRdModelInternal(T providedModel, int typeProviderId);
  }
}
