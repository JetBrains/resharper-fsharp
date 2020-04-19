using System;
using FSharp.Compiler;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedExprHostFactory : IOutOfProcessHostFactory<RdProvidedExprProcessModel>
  {
    private readonly IReadProvidedCache<Tuple<ProvidedExpr, int>> myProvidedExprsCache;
    private readonly IProvidedRdModelsCreator<ProvidedType, RdProvidedType> myProvidedTypeRdModelsCreator;

    public ProvidedExprHostFactory(IReadProvidedCache<Tuple<ProvidedExpr, int>> providedExprsCache,
      IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedTypeRdModelsCreator)
    {
      myProvidedExprsCache = providedExprsCache;
      myProvidedTypeRdModelsCreator = providedTypeRdModelsCreator;
    }

    public void Initialize(RdProvidedExprProcessModel model)
    {
      model.Type.Set(GetType);
    }

    private RdTask<int> GetType(Lifetime lifetime, int entityId)
    {
      var (providedExpr, typeProviderId) = myProvidedExprsCache.Get(entityId);
      var typeId = myProvidedTypeRdModelsCreator.CreateRdModel(providedExpr.Type, typeProviderId).EntityId;
      return RdTask<int>.Successful(typeId);
    }
  }
}
