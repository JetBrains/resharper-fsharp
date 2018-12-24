using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  [PolymorphicMarshaller(13)]
  public class FSharpCacheVersion
  {
    [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = r => new FSharpCacheVersion();
    [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => { };
  }
}
