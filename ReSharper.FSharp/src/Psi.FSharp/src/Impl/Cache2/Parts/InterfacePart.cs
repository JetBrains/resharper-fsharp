using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Parts
{
  internal class InterfacePart : FSharpTypeMembersOwnerTypePart, Interface.IInterfacePart
  {
    public InterfacePart(IFSharpTypeDeclaration declaration, ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
    {
    }

    public InterfacePart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpInterface(this);
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.Interface;
  }

  public class FSharpInterface : Interface
  {
    public FSharpInterface(IInterfacePart part) : base(part)
    {
    }

    protected override MemberDecoration Modifiers => myParts.GetModifiers();
  }
}