using System;
using System.Linq;
using System.Reflection;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core.CompilerServices;
using Microsoft.FSharp.Quotations;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyTypeProviderWithCache : ITypeProvider
  {
    private readonly RdTypeProvider myRdTypeProvider;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private RdTypeProviderProcessModel RdTypeProviderProcessModel => myProcessModel.RdTypeProviderProcessModel;
    private int EntityId => myRdTypeProvider.TypeProviderId;

    public ProxyTypeProviderWithCache(
      RdTypeProvider rdTypeProvider,
      RdFSharpTypeProvidersLoaderModel processModel)
    {
      myRdTypeProvider = rdTypeProvider;
      myProcessModel = processModel;

      myCache = new TypeProviderCache(myRdTypeProvider, myProcessModel);
      InitCaches();

      var lifeTime = new Lifetime(); //temp
      RdTypeProviderProcessModel.Invalidate.Advise(lifeTime, OnInvalidate);
    }

    private void OnInvalidate(int typeProviderId)
    {
      if (typeProviderId != EntityId) return;
      
      InitCaches();
      Invalidate?.Invoke(this, EventArgs.Empty);
    }

    public IProvidedNamespace[] GetNamespaces() => myProvidedNamespaces.Value;

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

    private void InitCaches()
    {
      // ReSharper disable once CoVariantArrayConversion
      myProvidedNamespaces = new Lazy<IProvidedNamespace[]>(() => RdTypeProviderProcessModel.GetNamespaces
        .Sync(EntityId)
        .Select(t => new ProxyProvidedNamespaceWithCache(t, myProcessModel, myCache))
        .ToArray());
      
      myCache.Invalidate();
    }
    
    public event EventHandler Invalidate;
    
    private readonly TypeProviderCache myCache;
    private Lazy<IProvidedNamespace[]> myProvidedNamespaces;
  }
}
