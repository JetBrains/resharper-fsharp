using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class ModulePartBase<T> : FSharpTypePart<T>, Class.IClassPart
    where T : class, IFSharpTypeElementDeclaration
  {
    protected ModulePartBase([NotNull] T declaration, [NotNull] string shortName, MemberDecoration memberDecoration,
      [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, shortName, memberDecoration, 0, cacheBuilder)
    {
    }

    protected ModulePartBase(IReader reader) : base(reader)
    {
    }

    public IEnumerable<ITypeMember> GetTypeMembers()
    {
      var declaration = GetDeclaration();
      if (declaration == null)
        return EmptyList<ITypeMember>.Instance;

      return declaration.MemberDeclarations.Select(d => d.DeclaredElement)
        .Where(el => el is IFSharpTypeMember fsMember && (!fsMember.IsFSharpMember || fsMember.IsExtensionMember) ||
                     el != null);
    }

    public IEnumerable<IDeclaredType> GetSuperTypes()
    {
      return new[] {GetBaseClassType()};
    }

    public IDeclaredType GetBaseClassType()
    {
      return GetPsiModule().GetPredefinedType().Object;
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

    public override IDeclaration GetTypeParameterDeclaration(int index)
    {
      throw new InvalidOperationException();
    }

    public override string GetTypeParameterName(int index)
    {
      throw new InvalidOperationException();
    }

    public override TypeParameterVariance GetTypeParameterVariance(int index)
    {
      throw new InvalidOperationException();
    }

    public override IEnumerable<IType> GetTypeParameterSuperTypes(int index)
    {
      throw new InvalidOperationException();
    }

    public override TypeParameterConstraintFlags GetTypeParameterConstraintFlags(int index)
    {
      throw new InvalidOperationException();
    }
  }
}