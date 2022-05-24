using System;
using System.Linq;
using System.Reflection;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using JetBrains.Util;
using JetBrains.Util.Concurrency;
using Microsoft.FSharp.Core.CompilerServices;
using Microsoft.FSharp.Quotations;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public class ProxyTypeProvider : IProxyTypeProvider
  {
    private readonly RdTypeProvider myRdTypeProvider;
    private readonly TypeProvidersContext myTypeProvidersContext;

    private RdTypeProviderProcessModel RdTypeProviderProcessModel =>
      myTypeProvidersContext.Connection.ProtocolModel.RdTypeProviderProcessModel;

    public int EntityId => myRdTypeProvider.EntityId;
    public RdProvidedEntityType EntityType => RdProvidedEntityType.TypeProvider;
    public RdCustomAttributeData[] Attributes => EmptyArray<RdCustomAttributeData>.Instance;

    public ProxyTypeProvider(RdTypeProvider rdTypeProvider, TypeProvidersContext typeProvidersContext,
      IPsiModule module)
    {
      ProjectModule = module;
      myRdTypeProvider = rdTypeProvider;
      myTypeProvidersContext = typeProvidersContext;

      // ReSharper disable once CoVariantArrayConversion
      myProvidedNamespaces = new InterruptibleLazy<IProvidedNamespace[]>(() =>
        myTypeProvidersContext.Connection
          .ExecuteWithCatch(() => RdTypeProviderProcessModel.GetProvidedNamespaces.Sync(EntityId, RpcTimeouts.Maximal))
          .Select(t => new ProxyProvidedNamespace(t, this, myTypeProvidersContext))
          .ToArray());
    }

    public void OnInvalidate()
    {
      DisposeProxy();
      Invalidate?.Invoke(this, EventArgs.Empty);
    }

    public bool IsGenerative
    {
      get => myIsGenerative;
      set
      {
        ContainsGenerativeTypes?.Invoke(this, EventArgs.Empty);
        myIsGenerative = value;
      }
    }

    public IProvidedNamespace[] GetNamespaces() => myProvidedNamespaces.Value;

    public ParameterInfo[] GetStaticParameters(Type typeWithoutArguments) =>
      throw new InvalidOperationException("GetStaticParameters should be unreachable");

    public Type ApplyStaticArguments(Type typeWithoutArguments, string[] typePathWithArguments,
      object[] staticArguments) =>
      throw new InvalidOperationException("ApplyStaticArguments should be unreachable");

    public FSharpExpr GetInvokerExpression(MethodBase syntheticMethodBase, FSharpExpr[] parameters) =>
      throw new InvalidOperationException("GetInvokerExpression should be unreachable");

    public byte[] GetGeneratedAssemblyContents(Assembly assembly) =>
      throw new InvalidOperationException("GetGeneratedAssemblyContents should be unreachable");

    public ParameterInfo[] GetStaticParametersForMethod(MethodBase methodWithoutArguments) =>
      throw new InvalidOperationException("GetStaticParametersForMethod should be unreachable");

    public MethodBase ApplyStaticArgumentsForMethod(MethodBase methodWithoutArguments, string methodNameWithArguments,
      object[] staticArguments) =>
      throw new InvalidOperationException("ApplyStaticArgumentsForMethod should be unreachable");

    public ProvidedExpr GetInvokerExpression(ProvidedMethodBase methodBase, ProvidedVar[] paramExprs) =>
      methodBase switch
      {
        ProvidedMethodInfo info =>
          new DummyProvidedExpr(info.ReturnType,
            ProvidedExprType.NewProvidedCallExpr(null, info, Array.Empty<ProvidedExpr>()), methodBase.Context),

        ProvidedConstructorInfo info =>
          new DummyProvidedExpr(ProvidedExprType.NewProvidedNewObjectExpr(info, Array.Empty<ProvidedExpr>()),
            methodBase.Context),

        { } info => throw new ArgumentException($"Unexpected MethodBase: {info}"),
        _ => throw new ArgumentException($"Unexpected MethodBase"),
      };

    public string GetDisplayName(bool fullName) => fullName ? myRdTypeProvider.FullName : myRdTypeProvider.Name;
    public IPsiModule ProjectModule { get; }

    public void Dispose()
    {
    }

    public void DisposeProxy()
    {
      if (myIsDisposed) return;

      myTypeProvidersContext.Dispose(this);

      myIsDisposed = true;
      Disposed?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler ContainsGenerativeTypes;
    public event EventHandler Invalidate;
    public event EventHandler Disposed;
    private bool myIsGenerative;
    private bool myIsDisposed;
    private readonly InterruptibleLazy<IProvidedNamespace[]> myProvidedNamespaces;
  }
}
