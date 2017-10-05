using JetBrains.Annotations;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util.Concurrency;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class RecordPart : SimpleTypePartBase, Class.IClassPart
  {
    private static readonly ClrTypeName CliMutableAttrTypeName =
      new ClrTypeName("Microsoft.FSharp.Core.CLIMutableAttribute");

    public RecordPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
    {
    }

    public RecordPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpRecord(this);
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.Record;

    public InterruptibleLazy<bool> IsCliMutable =>
      new InterruptibleLazy<bool>(() => HasAttributeInstance(CliMutableAttrTypeName));
  }
}