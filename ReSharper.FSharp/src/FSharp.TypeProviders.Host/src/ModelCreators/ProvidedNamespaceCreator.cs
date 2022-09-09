﻿using System.Linq;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.TypeProviders;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators
{
  public class ProvidedNamespaceCreator : ProvidedRdModelsCreatorBase<IProvidedNamespace, RdProvidedNamespace>
  {
    private readonly TypeProvidersContext myTypeProvidersContext;

    public ProvidedNamespaceCreator(TypeProvidersContext typeProvidersContext) =>
      myTypeProvidersContext = typeProvidersContext;

    protected override RdProvidedNamespace CreateRdModelInternal(IProvidedNamespace providedModel, int typeProviderId)
    {
      var types = providedModel
        .GetTypes()
        .Select(ProvidedType.CreateNoContext)
        .CreateIds(myTypeProvidersContext.ProvidedTypeRdModelsCreator, typeProviderId);

      var nestedNamespaces = providedModel
        .GetNestedNamespaces()
        .CreateRdModels(this, typeProviderId);

      return new RdProvidedNamespace(providedModel.NamespaceName, nestedNamespaces, types);
    }
  }
}
