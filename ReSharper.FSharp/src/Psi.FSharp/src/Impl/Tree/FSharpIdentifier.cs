using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.FSharp.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class FSharpIdentifier
  {
    public string Name =>
      IdentifierToken != null
        ? FSharpNamesUtil.RemoveBackticks(IdentifierToken.GetText())
        : SharedImplUtil.MISSING_DECLARATION_NAME;
  }
}