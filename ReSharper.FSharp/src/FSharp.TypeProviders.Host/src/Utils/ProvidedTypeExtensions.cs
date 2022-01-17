using System;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Utils
{
  public static class ProvidedTypeExtensions
  {
    public static bool IsCreatedByProvider(this ProvidedType providedType) =>
      IsCreatedByProvider(providedType.RawSystemType);

    public static bool IsCreatedByProvider(this Type type) =>
      // FSharp.TypeProviders.SDK/ProvidedTypes.fsi
      type.GetType().Name == "ProvidedTypeDefinition";
  }
}
