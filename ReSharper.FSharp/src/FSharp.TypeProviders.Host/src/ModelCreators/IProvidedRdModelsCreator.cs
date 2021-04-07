using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators
{
  public interface IProvidedRdModelsCreator<in T, out TU> where TU : class
  {
    TU CreateRdModel([CanBeNull] T providedModel, int typeProviderId);
  }

  public interface IProvidedRdModelsCreatorWithCache<in T, out TU, out TId> : IProvidedRdModelsCreator<T, TU>
    where TU : class
  {
    TId GetOrCreateId([CanBeNull] T providedModel, int typeProviderId);
  }
}
