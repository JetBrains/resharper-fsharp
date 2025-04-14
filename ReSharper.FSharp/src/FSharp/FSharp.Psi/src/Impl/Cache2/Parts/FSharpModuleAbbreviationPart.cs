using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  // todo: check these types
  ///   * abstract types (i.e., no representation in a signature file)
  ///   * units of measure
  internal class FSharpModuleAbbreviationPart : FSharpTypeMembersOwnerTypePart, Class.IClassPart
  {
    public FSharpModuleAbbreviationPart([NotNull] IFSharpTypeOldDeclaration declaration,
      [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder, PartKind.Class)
    {
    }

    public FSharpModuleAbbreviationPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpModuleAbbreviation(this);

    protected override byte SerializationTag => (byte) FSharpPartKind.ModuleAbbreviation;
  }
}
