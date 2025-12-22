using System;
using FSharp.Compiler.Syntax;
using FSharp.Compiler.SyntaxTrivia;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.TreeBuilder;
using Microsoft.FSharp.Collections;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public class ChameleonExpressionNodeType : CompositeNodeWithArgumentType
  {
    // Tokens start at 1000, generated elements at 2000, so let's just use 3000.
    public const int NodeIndex = 3000;

    public static readonly NodeType Instance = new ChameleonExpressionNodeType();

    private ChameleonExpressionNodeType() : base("F# Chameleon Expression Node Type", NodeIndex, typeof(ChameleonExpression))
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
