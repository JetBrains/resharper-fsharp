﻿using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class TopLevelModulePart : FSharpClassLikePart<ITopLevelModuleDeclaration>, Class.IClassPart
  {
    public TopLevelModulePart(ITopLevelModuleDeclaration declaration)
      : base(declaration, ModifiersUtil.GetDecoration(declaration.AccessModifiers))
    {
    }

    public TopLevelModulePart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpTopLevelModule(this);
    }

    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      return MemberPresenceFlag.NONE;
    }

    public override IDeclaredType GetBaseClassType()
    {
      return GetDeclaration()?.GetPsiModule().GetPredefinedType().Object;
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

    protected override byte SerializationTag => (byte) FSharpSerializationTag.ModulePart;
  }
}