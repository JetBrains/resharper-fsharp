using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class InterfacePart : FSharpTypeMembersOwnerTypePart, IFSharpInterfacePart
  {
    public InterfacePart([NotNull] IFSharpTypeOrExtensionDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder, PartKind.Interface)
    {
    }

    public InterfacePart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpInterface(this);

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.Interface;
  }
}
