using System;
using FSharp.Compiler;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IChameleonExpression : IChameleonNode
  {
    [CanBeNull] SyntaxTree.SynExpr SynExpr { get; }
    int OriginalStartOffset { get; }
    int OriginalLineStart { get; }

    bool Check(Func<IFSharpExpression, bool> fsExprPredicate, Func<SyntaxTree.SynExpr, bool> synExprPredicate);
  }
}
