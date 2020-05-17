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
      CreateRdModelInternal(ProvidedMethodInfo providedModel, int entityId)
    {
      var flags = RdProvidedMethodFlags.None;
      if (providedModel.IsGenericMethod) flags |= RdProvidedMethodFlags.IsGenericMethod;
      if (providedModel.IsStatic) flags |= RdProvidedMethodFlags.IsStatic;
      if (providedModel.IsFamily) flags |= RdProvidedMethodFlags.IsFamily;
      if (providedModel.IsFamilyAndAssembly) flags |= RdProvidedMethodFlags.IsFamilyAndAssembly;
      if (providedModel.IsFamilyOrAssembly) flags |= RdProvidedMethodFlags.IsFamilyOrAssembly;
      if (providedModel.IsVirtual) flags |= RdProvidedMethodFlags.IsVirtual;
      if (providedModel.IsFinal) flags |= RdProvidedMethodFlags.IsFinal;
      if (providedModel.IsPublic) flags |= RdProvidedMethodFlags.IsPublic;
      if (providedModel.IsAbstract) flags |= RdProvidedMethodFlags.IsAbstract;
      if (providedModel.IsHideBySig) flags |= RdProvidedMethodFlags.IsHideBySig;
      if (providedModel.IsConstructor) flags |= RdProvidedMethodFlags.IsConstructor;

      return new RdProvidedMethodInfo(providedModel.MetadataToken, flags, providedModel.Name, entityId);
    }
  }
}
