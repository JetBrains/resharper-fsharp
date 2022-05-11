using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
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

    public override TypeElement CreateTypeElement() => new FSharpClassOrProvidedTypeAbbreviation(this);

    protected override byte SerializationTag => (byte)FSharpPartKind.AbbreviationOrSingleCaseUnion;
  }

  internal class StructTypeAbbreviationOrDeclarationPart : TypeAbbreviationOrDeclarationPartBase, IFSharpStructPart
  {
    public StructTypeAbbreviationOrDeclarationPart([NotNull] IFSharpTypeDeclaration declaration,
      [NotNull] ICacheBuilder cacheBuilder) : base(declaration, cacheBuilder)
    {
    }

    public StructTypeAbbreviationOrDeclarationPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() => new FSharpStruct(this);
    protected override byte SerializationTag => (byte)FSharpPartKind.StructAbbreviationOrSingleCaseUnion;

    public override IDeclaredType GetBaseClassType() => null;

    public bool IsReadonly => false;
    public bool IsByRefLike => false;
  }

  internal abstract class TypeAbbreviationOrDeclarationPartBase : UnionPartBase, ITypeAbbreviationOrDeclarationPart
  {
    protected TypeAbbreviationOrDeclarationPartBase([NotNull] IFSharpTypeDeclaration declaration,
      [NotNull] ICacheBuilder cacheBuilder) : base(declaration, cacheBuilder, false, true)
    {
    }

    protected TypeAbbreviationOrDeclarationPartBase(IReader reader) : base(reader)
    {
    }

    public bool IsProvidedAndGenerated =>
      GetDeclaration() is { } decl &&
      decl.GetFcsSymbol() is FSharpEntity { IsProvidedAndGenerated: true };

    public bool IsUnionCase =>
      GetDeclaration() is IFSharpTypeDeclaration
      {
        TypeRepresentation: ITypeAbbreviationRepresentation { CanBeUnionCase: true }
      } decl &&
      decl.GetFcsSymbol() is FSharpEntity { IsFSharpUnion: true };
  }
}
