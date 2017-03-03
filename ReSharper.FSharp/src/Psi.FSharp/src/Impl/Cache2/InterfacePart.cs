using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class InterfacePart : FSharpObjectModelTypePart, Interface.IInterfacePart
  {
    public InterfacePart(IFSharpObjectModelTypeDeclaration declaration) : base(declaration)
    {
    }

    public InterfacePart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new Interface(this);
    }

    protected override byte SerializationTag => (byte) FSharpSerializationTag.InterfacePart;
  }
}