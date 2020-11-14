using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class TypeAbbreviationOrDeclarationPart : TypeAbbreviationOrDeclarationPartBase, Class.IClassPart
  {
    public TypeAbbreviationOrDeclarationPart([NotNull] IFSharpTypeDeclaration declaration,
      [NotNull] ICacheBuilder cacheBuilder) : base(declaration, cacheBuilder)
    {
    }

    public TypeAbbreviationOrDeclarationPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() => new FSharpClass(this);
    protected override byte SerializationTag => (byte) FSharpPartKind.AbbreviationOrSingleCaseUnion;
  }

  internal class StructTypeAbbreviationOrDeclarationPart : TypeAbbreviationOrDeclarationPartBase, Struct.IStructPart
  {
    public StructTypeAbbreviationOrDeclarationPart([NotNull] IFSharpTypeDeclaration declaration,
      [NotNull] ICacheBuilder cacheBuilder) : base(declaration, cacheBuilder)
    {
    }

    public StructTypeAbbreviationOrDeclarationPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() => new FSharpStruct(this);
    protected override byte SerializationTag => (byte) FSharpPartKind.StructAbbreviationOrSingleCaseUnion;

    public bool HasHiddenInstanceFields => false;
    public bool IsReadonly => false;
    public bool IsByRefLike => false;
  }

  internal abstract class TypeAbbreviationOrDeclarationPartBase : UnionPartBase
  {
    protected TypeAbbreviationOrDeclarationPartBase([NotNull] IFSharpTypeDeclaration declaration,
      [NotNull] ICacheBuilder cacheBuilder) : base(declaration, cacheBuilder, false, true)
    {
    }

    protected TypeAbbreviationOrDeclarationPartBase(IReader reader) : base(reader)
    {
    }

    public bool IsUnionCase =>
      GetDeclaration() is { } decl && decl.GetFSharpSymbol() is FSharpEntity entity && entity.IsFSharpUnion;
  }
}
