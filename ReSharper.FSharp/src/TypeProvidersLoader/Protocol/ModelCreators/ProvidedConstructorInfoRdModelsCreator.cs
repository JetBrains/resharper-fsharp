using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public class
    ProvidedConstructorInfoRdModelsCreator : ProvidedRdModelsCreatorBase<ProvidedConstructorInfo,
      RdProvidedConstructorInfo>
  {
    public ProvidedConstructorInfoRdModelsCreator(IWriteProvidedCache<Tuple<ProvidedConstructorInfo, int>> cache) :
      base(cache)
    {
    }

    protected override RdProvidedConstructorInfo CreateRdModelInternal(ProvidedConstructorInfo providedModel,
      int entityId) =>
      new RdProvidedConstructorInfo(providedModel.IsGenericMethod, providedModel.IsStatic, providedModel.IsFamily,
        providedModel.IsFamilyAndAssembly, providedModel.IsFamilyOrAssembly, providedModel.IsVirtual,
        providedModel.IsFinal, providedModel.IsPublic, providedModel.IsAbstract, providedModel.IsHideBySig,
        providedModel.IsConstructor, providedModel.Name, entityId);
  }
}
