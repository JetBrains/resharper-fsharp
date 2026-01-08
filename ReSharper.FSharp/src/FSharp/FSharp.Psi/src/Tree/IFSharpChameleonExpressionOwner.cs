namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public interface IFSharpChameleonExpressionOwner : IFSharpTreeNode
{
  IChameleonExpression ChameleonExpression { get; }
}
