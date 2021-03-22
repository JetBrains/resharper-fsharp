using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Checker;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface IFSharpTreeNode : ITreeNode
  {
    void Accept(TreeNodeVisitor visitor);
    void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context);
    TReturn Accept<TContext, TReturn>(TreeNodeVisitor<TContext, TReturn> visitor, TContext context);

    [NotNull] IFSharpFile FSharpFile { get; }
    [NotNull] FcsCheckerService CheckerService { get; }
  }
}
