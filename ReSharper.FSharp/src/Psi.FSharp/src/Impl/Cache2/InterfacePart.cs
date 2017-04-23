using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class InterfacePart : FSharpObjectModelTypePart, Interface.IInterfacePart
  {
    public InterfacePart(IFSharpTypeParametersOwnerDeclaration declaration, bool isHidden) : base(declaration, isHidden)
    {
    }

    public InterfacePart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new Interface(this);
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.Interface;
  }
}