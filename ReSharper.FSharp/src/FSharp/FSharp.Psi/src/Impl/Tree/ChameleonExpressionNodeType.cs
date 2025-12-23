using System;
using FSharp.Compiler.Syntax;
using FSharp.Compiler.SyntaxTrivia;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.TreeBuilder;
using Microsoft.FSharp.Collections;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  partial class ElementType
  {
    public static readonly CompositeNodeType CHAMELEON_EXPRESSION_WITH_ARG = new ChameleonExpressionNodeType();

    // Tokens start at 1000, generated elements at 2000, so let's just use 3000.
    public static readonly int CHAMELEON_EXPRESSION_WITH_ARG_NODE_TYPE_INDEX = 3000;

    private sealed class ChameleonExpressionNodeType : CompositeNodeWithArgumentType
    {
      public ChameleonExpressionNodeType() : base("F# Chameleon Expression Node Type", CHAMELEON_EXPRESSION_WITH_ARG_NODE_TYPE_INDEX, typeof(ChameleonExpression))
      {
      }

      public override CompositeElement Create() =>
        throw new InvalidOperationException("Should not be created without data.");

      public override CompositeElement Create(object data)
      {
        var (expr, warningDirectives, startOffset, lineStart) = (Tuple<SynExpr, FSharpList<WarnDirectiveTrivia>, int, int>) data;
        var chameleonExpression = new ChameleonExpression(expr, warningDirectives, startOffset, lineStart);
        return chameleonExpression;
      }
    }
  }
}
