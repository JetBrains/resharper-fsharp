using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class StructPart : StructuralTypePartBase, IFSharpStructPart
  {
    public StructPart([NotNull] IFSharpTypeOrExtensionDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder, PartKind.Struct)
    {
    }

    public StructPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpStruct(this);

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.Struct;

    public bool IsReadonly => false;
    public bool IsByRefLike => false;

    public override bool OverridesToString => false;
    public override bool ReportCtor => false;
  }
}
