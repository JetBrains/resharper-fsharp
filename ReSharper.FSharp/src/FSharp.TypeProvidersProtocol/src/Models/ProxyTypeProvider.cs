using System;
using System.Linq;
using System.Reflection;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core.CompilerServices;
using Microsoft.FSharp.Quotations;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyTypeProvider : ITypeProvider
  {
    private readonly RdTypeProvider myRdTypeProvider;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private RdTypeProviderProcessModel RdTypeProviderProcessModel => myProcessModel.RdTypeProviderProcessModel;
    private int EntityId => myRdTypeProvider.EntityId;

    public ProxyTypeProvider(
      RdTypeProvider rdTypeProvider,
      RdFSharpTypeProvidersLoaderModel processModel)
    {
      myRdTypeProvider = rdTypeProvider;
      myProcessModel = processModel;
      
      //myTypeProvider.Invalidate += TypeProviderOnInvalidate;
    }

    public IProvidedNamespace[] GetNamespaces() =>
      // ReSharper disable once CoVariantArrayConversion
      RdTypeProviderProcessModel.GetNamespaces
        .Sync(EntityId)
        .Select(t => new ProxyProvidedNamespace(t, myProcessModel))
        .ToArray();

    public ParameterInfo[] GetStaticParameters(Type typeWithoutArguments) =>
      throw new Exception("GetStaticParameters should be unreachable");

    public Type ApplyStaticArguments(Type typeWithoutArguments, string[] typePathWithArguments,
      object[] staticArguments) =>
      throw new Exception("ApplyStaticArguments should be unreachable");

    public FSharpExpr GetInvokerExpression(MethodBase syntheticMethodBase, FSharpExpr[] parameters) =>
      //typeProvider.GetInvokerExpression(syntheticMethodBase, parameters)
      throw new Exception("WHOAH! IS THIS GetInvokerExpression CALL?");

    public byte[] GetGeneratedAssemblyContents(Assembly assembly) =>
      throw new Exception("GetGeneratedAssemblyContents should be unreachable");

    public void Dispose() => RdTypeProviderProcessModel.Dispose.Sync(EntityId);

    public event EventHandler Invalidate;
  }
}
