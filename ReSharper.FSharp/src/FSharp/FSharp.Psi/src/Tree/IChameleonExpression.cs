using System;
using FSharp.Compiler.Syntax;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IChameleonExpression : IChameleonNode
  {
    [CanBeNull] SynExpr SynExpr { get; }
    int OriginalStartOffset { get; }
    int OriginalLineStart { get; }

    bool Check(Func<IFSharpExpression, bool> fsExprPredicate, Func<SynExpr, bool> synExprPredicate);
  }
}
