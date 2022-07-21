using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Modules;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public interface IProxyTypeProvider : ITypeProvider, ITypeProvider2, IRdProvidedEntity
  {
    ProvidedExpr GetInvokerExpression(ProvidedMethodBase methodBase, ProvidedVar[] paramExprs);
    string GetDisplayName(bool fullName);
    
    /// Can be null if type provider instantiated from F# script 
    [CanBeNull] IPsiModule PsiModule { get; }

    /// Before the proxy type provider receives the invalidation signal,
    /// the out-of-process caches and type provider will already be invalidated
    void OnInvalidate();

    bool IsGenerative { get; set; }
    event EventHandler ContainsGenerativeTypes;
    void DisposeProxy();
    event EventHandler Disposed;
  }
}
