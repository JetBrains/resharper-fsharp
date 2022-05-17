using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
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
        new DependentProvidedTypesCache<string, MakeGenericTypeArgs>(this, ProvidedTypeProtocol.MakeGenericType);
      AppliedProvidedTypesCache =
        new DependentProvidedTypesCache<string, ApplyStaticArgumentsParameters>(this,
          ProvidedTypeProtocol.ApplyStaticArguments);
      ArrayProvidedTypesCache =
        new DependentProvidedTypesCache<int, MakeArrayTypeArgs>(this, ProvidedTypeProtocol.MakeArrayType);
    }

    public TypeProvidersConnection Connection { get; }
    public ProvidedTypesCache ProvidedTypesCache { get; }
    public ProvidedAssembliesCache ProvidedAssembliesCache { get; }
    public DependentProvidedTypesCache<string, MakeGenericTypeArgs> GenericProvidedTypesCache { get; }
    public DependentProvidedTypesCache<string, ApplyStaticArgumentsParameters> AppliedProvidedTypesCache { get; }
    public DependentProvidedTypesCache<int, MakeArrayTypeArgs> ArrayProvidedTypesCache { get; }
    public IProvidedCustomAttributeProvider ProvidedCustomAttributeProvider { get; }
    public ProvidedAbbreviationsCache ProvidedAbbreviations { get; } = new();

    private RdProvidedTypeProcessModel ProvidedTypeProtocol => Connection.ProtocolModel.RdProvidedTypeProcessModel;

    public void Dispose(IProxyTypeProvider typeProvider)
    {
      var typeProviderId = typeProvider.EntityId;
      ProvidedTypesCache.Remove(typeProviderId);
      ProvidedAssembliesCache.Remove(typeProviderId);
      GenericProvidedTypesCache.Remove(typeProviderId);
      AppliedProvidedTypesCache.Remove(typeProviderId);
      ArrayProvidedTypesCache.Remove(typeProviderId);
      if (typeProvider.IsGenerative) ProvidedAbbreviations.Remove(typeProvider);
    }

    public string Dump() =>
      string.Join("\n\n", ProvidedTypesCache.Dump(), ProvidedAssembliesCache.Dump(), ProvidedAbbreviations.Dump());
  }
}
