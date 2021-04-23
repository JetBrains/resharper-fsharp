using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators
{
  public static class ModelCreatorsExtensions
  {
    public static TU[] CreateRdModels<T, TU>([NotNull] this IEnumerable<T> models,
      IProvidedRdModelsCreator<T, TU> rdModelsCreator, int typeProviderId) where TU : class =>
      models.Select(t => rdModelsCreator.CreateRdModel(t, typeProviderId)).ToArray();

    public static TId[] CreateIds<T, TU, TId>([NotNull] this IEnumerable<T> models,
      IProvidedRdModelsCreatorWithCache<T, TU, TId> rdModelsCreator, int typeProviderId) where TU : RdProvidedEntity =>
      models.Select(t => rdModelsCreator.GetOrCreateId(t, typeProviderId)).ToArray();
  }
}
