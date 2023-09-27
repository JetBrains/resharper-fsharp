using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ILetBindings
  {
    ITokenNode BindingKeyword { get; }

    bool IsUse { get; }
    bool IsRecursive { get; }
    void SetIsRecursive(bool value);
  }
}
