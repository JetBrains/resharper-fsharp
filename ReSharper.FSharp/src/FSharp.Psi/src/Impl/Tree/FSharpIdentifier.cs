using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class FSharpIdentifier
  {
    public string Name => GetText().RemoveBackticks();
  }
}