using System;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public interface IProxyTypeProvider : ITypeProvider, ITypeProvider2, IRdProvidedEntity
  {
    ProvidedExpr GetInvokerExpression(ProvidedMethodBase methodBase, ProvidedVar[] paramExprs);
    string GetDisplayName(bool fullName);
    void OnInvalidate();
    /// <summary>
    /// After removing an assembly (package) with a type provider from a project,
    /// FCS will not ask for providers from this assembly on a subsequent call to InstantiateTypeProvidersOfAssembly.
    /// We want to cache and reuse providers that have not changed,
    /// in order to do this, we need to determine whether the provider was removed from the project or not by whether it was requested.
    /// With each new request from FCS, we will increment the provider version, and with each call to provider.Dispose(), we will decrement it.
    /// If the version is less than 0, then the type provider is no longer in the project
    /// and will be removed from the caches when Dispose() is called.
    /// </summary>
    void IncrementVersion();
    bool IsInvalidated { get; }
    event EventHandler Disposed;
  }
}
