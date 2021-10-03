using System;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public interface IProxyTypeProvider : ITypeProvider, ITypeProvider2, IRdProvidedEntity
  {
    ProvidedExpr GetInvokerExpression(ProvidedMethodBase methodBase, ProvidedVar[] paramExprs);
    string GetDisplayName(bool fullName);
    void OnInvalidate();
    bool IsGenerative { get; set; }
    event EventHandler ContainsGenerativeTypes;
    void DisposeProxy();
    event EventHandler Disposed;
  }
}
