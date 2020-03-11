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
      CreateRdModelInternal(ProvidedMethodInfo providedNativeModel, int entityId) => new RdProvidedMethodInfo(
      providedNativeModel.MetadataToken,
      providedNativeModel.IsGenericMethod,
      providedNativeModel.IsStatic, providedNativeModel.IsFamily, providedNativeModel.IsFamilyAndAssembly,
      providedNativeModel.IsFamilyOrAssembly, providedNativeModel.IsVirtual, providedNativeModel.IsFinal,
      providedNativeModel.IsPublic,
      providedNativeModel.IsAbstract, providedNativeModel.IsHideBySig, providedNativeModel.IsConstructor,
      providedNativeModel.Name, entityId);
  }
}
