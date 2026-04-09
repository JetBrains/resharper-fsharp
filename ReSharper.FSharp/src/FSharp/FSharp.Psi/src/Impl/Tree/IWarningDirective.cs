using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

public static class WarningDirectiveExtensions
{
  public static bool IsNowarn(this IWarningDirective directive) =>
    directive.HashToken?.GetTokenType() == FSharpTokenType.PP_NOWARN;
}
