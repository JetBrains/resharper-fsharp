using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public interface IProxyTypeProvider : ITypeProvider, ITypeProvider2
  {
    ProvidedExpr GetInvokerExpression(ProvidedMethodBase methodBase, ProvidedVar[] paramExprs);
  }
}
