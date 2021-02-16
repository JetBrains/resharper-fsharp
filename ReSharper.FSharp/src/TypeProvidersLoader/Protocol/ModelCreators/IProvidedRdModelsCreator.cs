using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public interface IProvidedRdModelsCreator<in T, out Tu> where Tu : class
  {
    Tu CreateRdModel([CanBeNull] T providedModel, int typeProviderId);
  }

  public interface IProvidedRdModelsCreatorWithCache<in T, out Tu, out TId> : IProvidedRdModelsCreator<T, Tu>
    where Tu : class
  {
    TId GetOrCreateId([CanBeNull] T providedModel, int typeProviderId);
  }
}
