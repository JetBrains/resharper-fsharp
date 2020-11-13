using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class TypeRepresentationBase : FSharpCompositeElement
  {
    public IFSharpTypeDeclaration TypeDeclaration =>
      FSharpTypeDeclarationNavigator.GetByTypeRepresentation((ITypeRepresentation) this);
    
    public virtual PartKind TypePartKind => PartKind.Class;
  }
}
