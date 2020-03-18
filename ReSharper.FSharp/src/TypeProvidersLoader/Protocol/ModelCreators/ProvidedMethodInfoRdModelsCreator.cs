using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public class ProvidedMethodInfoRdModelsCreator : ProvidedRdModelsCreatorBase<ProvidedMethodInfo, RdProvidedMethodInfo>
  {
    public ProvidedMethodInfoRdModelsCreator(IWriteProvidedCache<Tuple<ProvidedMethodInfo, int>> cache) : base(cache)
    {
    }

    protected override RdProvidedMethodInfo
      CreateRdModelInternal(ProvidedMethodInfo providedModel, int entityId) => new RdProvidedMethodInfo(
      providedModel.MetadataToken,
      providedModel.IsGenericMethod,
      providedModel.IsStatic, providedModel.IsFamily, providedModel.IsFamilyAndAssembly,
      providedModel.IsFamilyOrAssembly, providedModel.IsVirtual, providedModel.IsFinal,
      providedModel.IsPublic,
      providedModel.IsAbstract, providedModel.IsHideBySig, providedModel.IsConstructor,
      providedModel.Name, entityId);
  }
}
