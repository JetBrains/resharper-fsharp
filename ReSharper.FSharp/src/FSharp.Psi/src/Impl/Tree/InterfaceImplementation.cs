using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class InterfaceImplementation
  {
    public override string CompiledName => NameIdentifier.GetSourceName();
    public override IFSharpIdentifier NameIdentifier => InterfaceType?.LongIdentifier;

    public override IDeclaredElement DeclaredElement => null;

    public TreeNodeCollection<ITypeParameterOfTypeDeclaration> TypeParameters =>
      TreeNodeCollection<ITypeParameterOfTypeDeclaration>.Empty;
  }
}
