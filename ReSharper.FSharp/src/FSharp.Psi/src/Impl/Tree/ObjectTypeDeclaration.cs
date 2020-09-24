using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ObjectTypeDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(AllAttributes);
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;

    public override PartKind TypePartKind
    {
      get
      {
        if (FSharpImplUtil.GetTypeKind(AllAttributes, out var typeKind))
          return typeKind;

        foreach (var member in TypeMembersEnumerable)
          if (!(member is IInterfaceInherit) && !(member is IAbstractMemberDeclaration))
            return PartKind.Class;

        return PartKind.Interface;
      }
    }
  }
}