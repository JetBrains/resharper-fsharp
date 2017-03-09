using JetBrains.ReSharper.Psi.FSharp.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class FSharpIdentifier
  {
    public string Name => FSharpNamesUtil.RemoveBackticks(IdentifierToken.GetText());
  }
}