using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  [PolymorphicMarshaller(33)]
  public class FSharpCacheVersion
  {
    [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = _ => new FSharpCacheVersion();
    [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (_, __) => { };
  }
}
