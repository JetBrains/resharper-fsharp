using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class InterfaceImplementation
  {
    public IFSharpIdentifier NameIdentifier => InterfaceType?.ReferenceName.Identifier;

    public TreeNodeCollection<ITypeParameterOfTypeDeclaration> TypeParameters =>
      TreeNodeCollection<ITypeParameterOfTypeDeclaration>.Empty;

    public override ITokenNode IdentifierToken =>
      NameIdentifier?.IdentifierToken;
  }
}
