﻿using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public class ProvidedConstructorInfoRdModelsCreator : ProvidedRdModelsCreatorBase<ProvidedConstructorInfo,
      RdProvidedConstructorInfo>
  {
    public ProvidedConstructorInfoRdModelsCreator(IWriteProvidedCache<Tuple<ProvidedConstructorInfo, int>> cache) :
      base(cache)
    {
    }

    protected override RdProvidedConstructorInfo CreateRdModelInternal(ProvidedConstructorInfo providedModel,
      int entityId)
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

      return new RdProvidedConstructorInfo(flags, providedModel.Name, entityId);
    }
  }
}
