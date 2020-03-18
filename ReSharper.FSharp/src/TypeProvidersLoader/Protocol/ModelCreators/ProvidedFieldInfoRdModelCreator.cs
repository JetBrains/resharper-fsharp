using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public class ProvidedFieldInfoRdModelCreator : ProvidedRdModelsCreatorBase<ProvidedFieldInfo, RdProvidedFieldInfo>
  {
    public ProvidedFieldInfoRdModelCreator(IWriteProvidedCache<Tuple<ProvidedFieldInfo, int>> cache) : base(cache)
    {
    }

    protected override RdProvidedFieldInfo CreateRdModelInternal(ProvidedFieldInfo providedModel, int entityId) =>
      new RdProvidedFieldInfo(providedModel.IsInitOnly, providedModel.IsStatic, providedModel.IsSpecialName,
        providedModel.IsLiteral, providedModel.GetRawConstantValue().BoxToClientStaticArg(), providedModel.IsPublic,
        providedModel.IsFamily,
        providedModel.IsFamilyAndAssembly, providedModel.IsFamilyOrAssembly, providedModel.IsPrivate,
        providedModel.Name, entityId);
  }
}
