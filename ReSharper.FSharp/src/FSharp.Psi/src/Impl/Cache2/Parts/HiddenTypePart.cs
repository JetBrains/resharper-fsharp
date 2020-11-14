using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  /// Used for the following things
  ///   * abstract types (i.e. no representation in a signature file)
  ///   * units of measure
  internal class HiddenTypePart : FSharpTypeMembersOwnerTypePart, Class.IClassPart
  {
    public HiddenTypePart([NotNull] IFSharpTypeOldDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
    {
    }

    public HiddenTypePart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpClass(this);

    public override MemberPresenceFlag GetMemberPresenceFlag() =>
      MemberPresenceFlag.NONE;

    public override MemberDecoration Modifiers =>
      MemberDecoration.FromModifiers(ReSharper.Psi.Modifiers.INTERNAL); // should not be accessible from other languages

    protected override byte SerializationTag => (byte) FSharpPartKind.HiddenType;
  }
}
