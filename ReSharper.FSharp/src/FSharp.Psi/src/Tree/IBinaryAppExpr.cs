using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IBinaryAppExpr
  {
    [NotNull] string ShortName { get; }
  }
}
