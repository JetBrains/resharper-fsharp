using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public static class ModelCreatorsExtensions
  {
    public static TU[] CreateRdModels<T, TU>([NotNull] this IEnumerable<T> models,
      IProvidedRdModelsCreator<T, TU> rdModelsCreator, int typeProviderId) where TU : class
    {
      var modelsArray = models is T[] array ? array : models.ToArray();
      var rdModels = new TU[modelsArray.Length];

      var i = 0;
      foreach (var model in modelsArray)
      {
        rdModels[i] = rdModelsCreator.CreateRdModel(model, typeProviderId);
        ++i;
      }

      return rdModels;
    }

    public static int[] CreateRdModelsAndReturnIds<T, TU>([NotNull] this IEnumerable<T> models,
      IProvidedRdModelsCreator<T, TU> rdModelsCreator, int typeProviderId) where TU : RdProvidedEntity
    {
      var modelsArray = models is T[] array ? array : models.ToArray();
      var rdModels = new int[modelsArray.Length];

      var i = 0;
      foreach (var model in modelsArray)
      {
        rdModels[i] = rdModelsCreator.CreateRdModel(model, typeProviderId).EntityId;
        ++i;
      }

      return rdModels;
    }
  }
}
