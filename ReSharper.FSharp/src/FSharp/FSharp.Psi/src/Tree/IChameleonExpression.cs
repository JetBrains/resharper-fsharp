using System;
using FSharp.Compiler.Syntax;
using FSharp.Compiler.SyntaxTrivia;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Collections;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IChameleonExpression : IChameleonNode
  {
    [CanBeNull] SynExpr SynExpr { get; }
    [NotNull] FSharpList<WarnDirectiveTrivia> WarningDirectives { get; }
    int OriginalStartOffset { get; }
    int OriginalLineStart { get; }

    bool Check(Func<IFSharpExpression, bool> fsExprPredicate, Func<SynExpr, bool> synExprPredicate);
  }
}
