using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using IProvidedCustomAttributeProvider =
  JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils.IProvidedCustomAttributeProvider;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache
{
  public class TypeProvidersContext
  {
    public TypeProvidersContext(TypeProvidersConnection connection)
    {
      Connection = connection;
      ProvidedCustomAttributeProvider = new ProvidedCustomAttributeProvider(connection);
      ProvidedTypesCache = new ProvidedTypesCache(this);
      ProvidedAssembliesCache = new ProvidedAssembliesCache(this);
      GenericProvidedTypesCache =
        new DependedProvidedTypesCache<string, MakeGenericTypeArgs>(this, ProvidedTypeProtocol.MakeGenericType);
      AppliedProvidedTypesCache =
        new DependedProvidedTypesCache<string, ApplyStaticArgumentsParameters>(this,
          ProvidedTypeProtocol.ApplyStaticArguments);
      ArrayProvidedTypesCache =
        new DependedProvidedTypesCache<int, MakeArrayTypeArgs>(this, ProvidedTypeProtocol.MakeArrayType);
    }

    public TypeProvidersConnection Connection { get; }
    public ProvidedTypesCache ProvidedTypesCache { get; }
    public ProvidedAssembliesCache ProvidedAssembliesCache { get; }
    public DependedProvidedTypesCache<string, MakeGenericTypeArgs> GenericProvidedTypesCache { get; }
    public DependedProvidedTypesCache<string, ApplyStaticArgumentsParameters> AppliedProvidedTypesCache { get; }
    public DependedProvidedTypesCache<int, MakeArrayTypeArgs> ArrayProvidedTypesCache { get; }
    public IProvidedCustomAttributeProvider ProvidedCustomAttributeProvider { get; }

    private RdProvidedTypeProcessModel ProvidedTypeProtocol => Connection.ProtocolModel.RdProvidedTypeProcessModel;

    public void Dispose(int typeProviderId)
    {
      ProvidedTypesCache.Remove(typeProviderId);
      ProvidedAssembliesCache.Remove(typeProviderId);
      GenericProvidedTypesCache.Remove(typeProviderId);
      AppliedProvidedTypesCache.Remove(typeProviderId);
      ArrayProvidedTypesCache.Remove(typeProviderId);
    }

    public string Dump() =>
      string.Join("\n\n", ProvidedTypesCache.Dump(), ProvidedAssembliesCache.Dump());
  }
}
