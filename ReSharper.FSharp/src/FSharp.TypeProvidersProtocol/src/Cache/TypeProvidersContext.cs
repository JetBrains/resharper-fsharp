using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using static FSharp.Compiler.ExtensionTyping;
using IProvidedCustomAttributeProvider =
  JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils.IProvidedCustomAttributeProvider;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache
{
  public class TypeProvidersContext
  {
    public TypeProvidersContext(TypeProvidersConnection connection)
    {
      Connection = connection;
      ProvidedCustomAttributeProvider = new ProvidedCustomAttributeProvider(connection);
      ProvidedTypesCache = new ProvidedTypesCache(this);
      ProvidedAssembliesCache = new ProvidedAssembliesCache(this);
    }

    public TypeProvidersConnection Connection { get; }
    public IProvidedEntitiesCache<ProvidedType, int> ProvidedTypesCache { get; }
    public IProvidedEntitiesCache<ProvidedAssembly, int> ProvidedAssembliesCache { get; }
    public IProvidedCustomAttributeProvider ProvidedCustomAttributeProvider { get; }

    public void Dispose(int typeProviderId)
    {
      ProvidedTypesCache.Remove(typeProviderId);
      ProvidedAssembliesCache.Remove(typeProviderId);
    }

    public string Dump() =>
      string.Join("\n\n", ProvidedTypesCache.Dump(), ProvidedAssembliesCache.Dump());
  }
}
