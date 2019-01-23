using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface INamedPat
  {
    TreeOffset GetOffset();
  }
}
