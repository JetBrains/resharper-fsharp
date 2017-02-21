using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class ModulePart : FSharpClassLikePart<IModuleDeclaration>, Class.IClassPart
  {
    public ModulePart(IModuleDeclaration declaration) : base(declaration, declaration.ShortName)
    {
    }

    public ModulePart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpModule(this);
    }

    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      return MemberPresenceFlag.NONE;
    }

    public override IDeclaredType GetBaseClassType()
    {
      return GetDeclaration()?.GetPsiModule().GetPredefinedType().Object;
    }

    protected override byte SerializationTag => (byte) FSharpSerializationTag.ModulePart;
  }
}