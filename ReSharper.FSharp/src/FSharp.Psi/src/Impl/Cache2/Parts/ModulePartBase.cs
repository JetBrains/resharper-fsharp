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
  internal abstract class ModulePartBase<T> : FSharpTypePart<T>, IModulePart
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

    public override TypeElement CreateTypeElement() =>
      new FSharpModule(this);

    public IEnumerable<ITypeMember> GetTypeMembers() =>
      GetDeclaration() is { } declaration
        ? declaration.MemberDeclarations.Select(d => d.DeclaredElement).WhereNotNull()
        : EmptyList<ITypeMember>.Instance;

    public IEnumerable<IDeclaredType> GetSuperTypes() => new[] {GetBaseClassType()};
    public IDeclaredType GetBaseClassType() => GetPsiModule().GetPredefinedType().Object;

    public MemberPresenceFlag GetMemberPresenceFlag() => MemberPresenceFlag.NONE;

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

    public override IDeclaration GetTypeParameterDeclaration(int index) => throw new InvalidOperationException();
    public override string GetTypeParameterName(int index) => throw new InvalidOperationException();
    public override TypeParameterVariance GetTypeParameterVariance(int index) => throw new InvalidOperationException();
    public override IEnumerable<IType> GetTypeParameterSuperTypes(int index) => throw new InvalidOperationException();

    public override TypeParameterConstraintFlags GetTypeParameterConstraintFlags(int index) =>
      throw new InvalidOperationException();

    public virtual bool IsAnonymous => false;
    public abstract ModuleMembersAccessKind AccessKind { get; }

    public ITypeElement AssociatedTypeElement =>
      GetDeclaration() is INestedModuleDeclaration moduleDeclaration
        ? ((ITypeDeclaration) moduleDeclaration.GetAssociatedTypeDeclaration(out _)).DeclaredElement
        : null;
  }
}
