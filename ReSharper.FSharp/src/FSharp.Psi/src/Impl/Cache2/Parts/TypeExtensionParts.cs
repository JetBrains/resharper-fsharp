using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class ClassExtensionPart : FSharpTypeMembersOwnerTypePart, Class.IClassPart
  {
    public ClassExtensionPart([NotNull] IFSharpTypeOldDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
    {
    }

    public ClassExtensionPart(IReader reader) : base(reader)
    {
    }

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.ClassExtension;

    public override TypeElement CreateTypeElement() =>
      new FSharpClass(this);

    public override IEnumerable<ITypeElement> GetSuperTypeElements() => EmptyList<ITypeElement>.Instance;
  }

  internal class StructExtensionPart : FSharpTypeMembersOwnerTypePart, Struct.IStructPart
  {
    public StructExtensionPart([NotNull] IFSharpTypeOldDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
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

    public bool HasHiddenInstanceFields => false;
    public bool IsReadonly => false;
    public bool IsByRefLike => false;

    public override IEnumerable<ITypeElement> GetSuperTypeElements() => EmptyList<ITypeElement>.Instance;
  }
}
