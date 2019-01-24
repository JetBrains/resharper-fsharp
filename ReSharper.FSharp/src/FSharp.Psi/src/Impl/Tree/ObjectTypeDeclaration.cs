using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ObjectTypeDeclaration
  {
    protected override string DeclaredElementName => Identifier.GetCompiledName(Attributes);
    public override IFSharpIdentifier NameIdentifier => Identifier;

    public FSharpPartKind TypePartKind
    {
      get
      {
        if (FSharpImplUtil.GetTypeKind(AttributesEnumerable, out var typeKind))
          return typeKind;

        foreach (var member in TypeMembersEnumerable)
          if (!(member is IInterfaceInherit) && !(member is IAbstractSlot))
            return FSharpPartKind.Class;

        return FSharpPartKind.Interface;
      }
    }
  }
}