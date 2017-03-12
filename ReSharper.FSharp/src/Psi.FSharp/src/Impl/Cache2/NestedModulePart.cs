using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class NestedModulePart : FSharpClassLikePart<INestedModuleDeclaration>, Class.IClassPart
  {
    public NestedModulePart(INestedModuleDeclaration declaration)
      : base(declaration, ModifiersUtil.GetDecoration(declaration.AccessModifiers))
    {
    }

    public NestedModulePart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpNestedModule(this);
    }

    public override IDeclaredType GetBaseClassType()
    {
      return GetDeclaration()?.GetPsiModule().GetPredefinedType().Object;
    }

    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      return MemberPresenceFlag.NONE;
    }

    public override MemberDecoration Modifiers
    {
      get
      {
        var modifiers = base.Modifiers;
        modifiers.IsAbstract = true;
        modifiers.IsSealed = true;
        modifiers.IsStatic = true;

        return modifiers;
      }
    }

    protected override byte SerializationTag => (byte) FSharpSerializationTag.NestedModulePart;
  }
}