using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class ClassExtensionPart : FSharpTypeMembersOwnerTypePart, IFSharpClassPart
  {
    public ClassExtensionPart([NotNull] IFSharpTypeOrExtensionDeclaration declaration,
      [NotNull] ICacheBuilder cacheBuilder) : base(declaration, cacheBuilder, PartKind.Class)
    {
    }

    public ClassExtensionPart(IReader reader) : base(reader)
    {
    }

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.ClassExtension;

    public override TypeElement CreateTypeElement() =>
      new FSharpClass(this);

    public IClass GetSuperClass() => null;
    public override IEnumerable<ITypeElement> GetSuperTypeElements() => EmptyList<ITypeElement>.Instance;
  }

  internal class StructExtensionPart : FSharpTypeMembersOwnerTypePart, IFSharpStructPart
  {
    public StructExtensionPart([NotNull] IFSharpTypeOrExtensionDeclaration declaration,
      [NotNull] ICacheBuilder cacheBuilder) : base(declaration, cacheBuilder, PartKind.Struct)
    {
    }

    public StructExtensionPart(IReader reader) : base(reader)
    {
    }

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.StructExtension;

    public override TypeElement CreateTypeElement() =>
      new FSharpStruct(this);

    public override MemberPresenceFlag GetMemberPresenceFlag() =>
      MemberPresenceFlag.NONE;

    public bool IsReadonly => false;
    public bool IsByRefLike => false;

    public override IEnumerable<ITypeElement> GetSuperTypeElements() => EmptyList<ITypeElement>.Instance;
  }
}
