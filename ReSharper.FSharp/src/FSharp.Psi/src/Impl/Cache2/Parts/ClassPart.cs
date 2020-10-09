using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class ClassPart : FSharpTypeMembersOwnerTypePart, IFSharpClassPart
  {
    public ClassPart([NotNull] IFSharpTypeOldDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
    {
    }

    public ClassPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpClass(this);

    public override MemberPresenceFlag GetMemberPresenceFlag()
    {
      // todo: check actual members
      return base.GetMemberPresenceFlag() |
             MemberPresenceFlag.INSTANCE_CTOR |
             MemberPresenceFlag.IMPLICIT_OP;
    }

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.Class;

    [CanBeNull] internal IClrTypeName BaseTypeClrTypeName;

    public virtual IClass GetSuperClass()
    {
      if (BaseTypeClrTypeName != null)
        return BaseTypeClrTypeName.CreateTypeByClrName(GetPsiModule()).GetTypeElement() as IClass;

      var typeElement = GetBaseClassType()?.GetTypeElement();
      if (typeElement == null)
      {
        BaseTypeClrTypeName = EmptyClrTypeName.Instance;
        return null;
      }

      BaseTypeClrTypeName = typeElement.GetClrName().GetPersistent();
      return typeElement as IClass;
    }
  }
}
