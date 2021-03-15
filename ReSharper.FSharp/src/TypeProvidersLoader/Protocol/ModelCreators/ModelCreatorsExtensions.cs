using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public static class ModelCreatorsExtensions
  {
    public static Tu[] CreateRdModels<T, Tu>([NotNull] this IEnumerable<T> models,
      IProvidedRdModelsCreator<T, Tu> rdModelsCreator, int typeProviderId) where Tu : class =>
      models.Select(t => rdModelsCreator.CreateRdModel(t, typeProviderId)).ToArray();

    public static TId[] CreateIds<T, Tu, TId>([NotNull] this IEnumerable<T> models,
      IProvidedRdModelsCreatorWithCache<T, Tu, TId> rdModelsCreator, int typeProviderId) where Tu : RdProvidedEntity =>
      models.Select(t => rdModelsCreator.GetOrCreateId(t, typeProviderId)).ToArray();
  }
}
