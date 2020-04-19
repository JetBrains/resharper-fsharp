using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public interface IProxyTypeProvider
  {
    ProvidedExpr GetInvokerExpression(ProvidedMethodBase methodBase, ProvidedVar[] paramExprs);
  }
}
