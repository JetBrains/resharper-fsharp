using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public partial interface IFieldPat
{
  [NotNull] string ShortName { get; }
}
