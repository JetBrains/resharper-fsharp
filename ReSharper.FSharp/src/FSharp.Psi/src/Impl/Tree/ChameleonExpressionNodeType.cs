using System;
using FSharp.Compiler;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.TreeBuilder;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public class ChameleonExpressionNodeType : CompositeNodeWithArgumentType
  {
    // Tokens start at 1000, generated elements at 2000, so let's just use 3000.
    public const int NodeIndex = 3000;

    public static readonly NodeType Instance = new ChameleonExpressionNodeType();

    public ChameleonExpressionNodeType() : base("F# Chameleon Expression Node Type", NodeIndex)
    {
    }

    public override CompositeElement Create() =>
      throw new InvalidOperationException("Should not be created without data.");

    public override CompositeElement Create(object data) =>
      new ChameleonExpression(data as SyntaxTree.SynExpr);
  }
}
