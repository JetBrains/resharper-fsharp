using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Parts
{
  internal class ClassPart : FSharpTypeMembersOwnerTypePart, Class.IClassPart
  {
    public ClassPart(IFSharpTypeDeclaration declaration, ICacheBuilder cacheBuilder) : base(declaration, cacheBuilder)
    {
    }

    public ClassPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpClass(this);
    }

    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      // todo: check actual members 
      return MemberPresenceFlag.INSTANCE_CTOR;
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.Class;
  }

  public class FSharpClass : Class
  {
    public FSharpClass([NotNull] IClassPart part) : base(part)
    {
    }

    protected override MemberDecoration Modifiers => myParts.GetModifiers();
  }
}