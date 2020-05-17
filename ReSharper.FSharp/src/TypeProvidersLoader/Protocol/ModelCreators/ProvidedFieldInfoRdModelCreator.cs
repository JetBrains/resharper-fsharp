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

    protected override RdProvidedFieldInfo CreateRdModelInternal(ProvidedFieldInfo providedModel, int entityId)
    {
      var flags = RdProvidedFieldFlags.None;
      if (providedModel.IsInitOnly) flags |= RdProvidedFieldFlags.IsInitOnly;
      if (providedModel.IsStatic) flags |= RdProvidedFieldFlags.IsStatic;
      if (providedModel.IsSpecialName) flags |= RdProvidedFieldFlags.IsSpecialName;
      if (providedModel.IsLiteral) flags |= RdProvidedFieldFlags.IsLiteral;
      if (providedModel.IsPublic) flags |= RdProvidedFieldFlags.IsPublic;
      if (providedModel.IsFamily) flags |= RdProvidedFieldFlags.IsFamily;
      if (providedModel.IsFamilyAndAssembly) flags |= RdProvidedFieldFlags.IsFamilyAndAssembly;
      if (providedModel.IsFamilyOrAssembly) flags |= RdProvidedFieldFlags.IsFamilyOrAssembly;
      if (providedModel.IsPrivate) flags |= RdProvidedFieldFlags.IsPrivate;

      return new RdProvidedFieldInfo(providedModel.GetRawConstantValue().BoxToClientStaticArg(), flags,
        providedModel.Name, entityId);
    }
  }
}
