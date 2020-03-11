using JetBrains.Annotations;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public interface IProvidedRdModelsCreator<in T, out TU> where TU : class
  {
    TU CreateRdModel([CanBeNull] T providedModel, int typeProviderId);
  }
}
