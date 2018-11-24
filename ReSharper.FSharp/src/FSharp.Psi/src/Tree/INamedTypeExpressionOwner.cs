namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
    public interface INamedTypeExpressionOwner : IFSharpTypeMemberDeclaration
    {
        INamedTypeExpression TypeExpression { get; }
    }
}