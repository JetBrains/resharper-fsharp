using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  [PolymorphicMarshaller(16)]
  public class FSharpCacheVersion
  {
    [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = r => new FSharpCacheVersion();
    [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => { };
  }
}
