using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol
{
  public static class ProvidedTypesExtensions
  {
    [ContractAnnotation("null => null")]
    public static ProvidedType WithCache(this ProvidedType providedType) =>
      providedType == null ? null : new ProvidedTypeWithCache(providedType);

    public static ProvidedType[] WithCache(this ProvidedType[] providedTypes) =>
      providedTypes.Select(t => t.WithCache()).ToArray();

    public static ProvidedParameterInfo WithCache(this ProvidedParameterInfo parameterInfo) =>
      new ProvidedParameterInfoWithCache(parameterInfo);

    public static ProvidedParameterInfo[] WithCache(this ProvidedParameterInfo[] parameterInfos) =>
      parameterInfos.Select(t => t.WithCache()).ToArray();
  }
}
