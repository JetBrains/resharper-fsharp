using System;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedFieldInfoHostFactory : IOutOfProcessHostFactory<RdProvidedFieldInfoProcessModel>
  {
    private readonly IReadProvidedCache<Tuple<ProvidedFieldInfo, int>> myProvidedFieldInfosCache;
    private readonly IProvidedRdModelsCreator<ProvidedType, RdProvidedType> myProvidedRdModelsCreator;

    public ProvidedFieldInfoHostFactory(IReadProvidedCache<Tuple<ProvidedFieldInfo, int>> providedFieldInfosCache,
      IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedRdModelsCreator)
    {
      myProvidedFieldInfosCache = providedFieldInfosCache;
      myProvidedRdModelsCreator = providedRdModelsCreator;
    }

    public void Initialize(RdProvidedFieldInfoProcessModel model)
    {
      model.FieldType.Set(GetFieldType);
      model.DeclaringType.Set(GetDeclaringType);
    }

    private int GetDeclaringType(int entityId)
    {
      var (providedType, typeProviderId) = myProvidedFieldInfosCache.Get(entityId);
      return myProvidedRdModelsCreator.CreateRdModel(providedType.DeclaringType, typeProviderId).EntityId;
    }

    private int GetFieldType(int entityId)
    {
      var (providedType, typeProviderId) = myProvidedFieldInfosCache.Get(entityId);
      return myProvidedRdModelsCreator.CreateRdModel(providedType.FieldType, typeProviderId).EntityId;
    }
  }
}
