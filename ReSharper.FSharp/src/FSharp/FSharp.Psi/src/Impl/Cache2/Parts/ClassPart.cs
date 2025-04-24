using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class ClassPart : FSharpTypeMembersOwnerTypePart, IFSharpClassPart
  {
    public ClassPart([NotNull] IFSharpTypeOrExtensionDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder, PartKind.Class)
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

    [CanBeNull] internal FcsTypeMappingUtil.FcsTypeClrName BaseTypeClrTypeName;

    public virtual IClass GetSuperClass()
    {
      if (BaseTypeClrTypeName != null)
        return BaseTypeClrTypeName.GetTypeElement() as IClass;

      var typeElement = GetBaseClassType()?.GetTypeElement() as IClass;
      if (typeElement != null)
        BaseTypeClrTypeName = new FcsTypeMappingUtil.FcsTypeClrName(typeElement, GetPsiModule());;

      return typeElement;
    }
  }
}
