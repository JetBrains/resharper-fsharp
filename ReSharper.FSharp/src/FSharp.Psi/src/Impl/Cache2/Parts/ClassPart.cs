using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class ClassPart : FSharpTypeMembersOwnerTypePart, Class.IClassPart
  {
    public ClassPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
    {
    }

    public ClassPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpClass(this);

    public override MemberPresenceFlag GetMemberPresenceFlag()
    {
      // todo: check actual members
      return base.GetMemberPresenceFlag() |
             MemberPresenceFlag.INSTANCE_CTOR |
             MemberPresenceFlag.IMPLICIT_OP;
    }

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.Class;
  }
}