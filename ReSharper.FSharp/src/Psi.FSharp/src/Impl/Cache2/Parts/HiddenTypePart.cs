using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  /// <summary>
  /// Used for type abbreviations and abstract types
  /// todo: check cases:
  ///   * single union case without bar (parsed as abbreviation)
  ///   * units of measure
  ///   * provided types (cache them in assembly signature?)
  /// </summary>
  internal class HiddenTypePart : FSharpClassLikePart<IFSharpTypeDeclaration>, Class.IClassPart
  {
    public HiddenTypePart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, ModifiersUtil.GetDecoration(declaration.AccessModifiers, declaration.AttributesEnumerable),
        declaration.TypeParameters, cacheBuilder)
    {
    }

    public HiddenTypePart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new Class(this);
    }

    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      return MemberPresenceFlag.NONE;
    }

    public override MemberDecoration Modifiers =>
      MemberDecoration.FromModifiers(ReSharper.Psi.Modifiers.INTERNAL); // should not be accessible from other languages

    protected override byte SerializationTag => (byte) FSharpPartKind.HiddenType;
  }
}