using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;

internal class ILAssemblyTypeAbbreviationPart : TypeAbbreviationOrDeclarationPartBase, Class.IClassPart
{
    public ILAssemblyTypeAbbreviationPart([NotNull] IFSharpTypeDeclaration declaration,
        [NotNull] ICacheBuilder cacheBuilder) : base(declaration, cacheBuilder, PartKind.Class)
    {
    }

    public ILAssemblyTypeAbbreviationPart(IReader reader) : base(reader)
    {
    }

    protected override byte SerializationTag => (byte)FSharpPartKind.ILAssemblyAbbreviation;
    public override TypeElement CreateTypeElement() => new FSharpClass(this);
}
