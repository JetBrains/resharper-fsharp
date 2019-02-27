using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.dataStructures;
using JetBrains.Util.DataStructures;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpTypeMember<TDeclaration> : FSharpCachedTypeMemberBase<TDeclaration>, IFSharpTypeMember
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    protected FSharpTypeMember([NotNull] IDeclaration declaration) : base(declaration)
    {
    }

    public ITypeMember GetContainingTypeMember() =>
      (ITypeMember) GetContainingType();

    public override IList<IDeclaration> GetDeclarations() =>
      GetPartialDeclarations(null);

    public override IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile) =>
      GetPartialDeclarations(sourceFile);

    private IList<IDeclaration> GetPartialDeclarations([CanBeNull] IPsiSourceFile sourceFile)
    {
      var containingType = GetContainingType();
      if (containingType == null)
        return EmptyList<IDeclaration>.InstanceList;

      var declaration = GetDeclaration();
      if (declaration == null)
        return EmptyList<IDeclaration>.InstanceList;

      var list = new FrugalLocalList<IDeclaration>();
      var declarations =
        sourceFile != null
          ? containingType.GetDeclarationsIn(sourceFile)
          : containingType.GetDeclarations();

      foreach (var partDeclaration in declarations)
      {
        if (!(partDeclaration is IFSharpTypeElementDeclaration typeDeclaration))
          continue;

        foreach (var member in typeDeclaration.MemberDeclarations)
          if (member.DeclaredName == declaration.DeclaredName && Equals(this, member.DeclaredElement))
            list.Add(member);
      }

      return list.AsIList();
    }

    public override HybridCollection<IPsiSourceFile> GetSourceFiles() =>
      GetContainingType()?.GetSourceFiles() ??
      HybridCollection<IPsiSourceFile>.Empty;

    public override bool HasDeclarationsIn(IPsiSourceFile sourceFile) =>
      GetSourceFiles().Contains(sourceFile);

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(this, obj))
        return true;

      if (!(obj is FSharpTypeMember<TDeclaration> member)) return false;

      if (!ShortName.Equals(member.ShortName))
        return false;

      return Equals(GetContainingType(), member.GetContainingType());
    }

    public override int GetHashCode() => ShortName.GetHashCode();

    public virtual bool HasAttributeInstance(IClrTypeName clrName, bool inherit) => false;

    public virtual IList<IAttributeInstance> GetAttributeInstances(bool inherit) =>
      EmptyList<IAttributeInstance>.Instance;

    public virtual IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, bool inherit) =>
      EmptyList<IAttributeInstance>.Instance;

    public virtual AccessRights GetAccessRights() =>
      GetDeclaration()?.GetAccessRights() ?? AccessRights.PUBLIC;

    // todo
    public virtual bool IsAbstract => false;
    public virtual bool IsSealed => false;
    public virtual bool IsVirtual => false;
    public virtual bool IsOverride => false;
    public virtual bool IsStatic => false;
    public virtual bool IsReadonly => false;
    public virtual bool IsExtern => false;
    public virtual bool IsUnsafe => false;
    public virtual bool IsVolatile => false;

    public string XMLDocId => XMLDocUtil.GetTypeMemberXmlDocId(this, ShortName);

    public IList<TypeMemberInstance> GetHiddenMembers() => HiddenMemberImpl.GetHiddenMembers(this);

    public AccessibilityDomain AccessibilityDomain =>
      new AccessibilityDomain(AccessibilityDomain.AccessibilityDomainType.PUBLIC, null);

    public MemberHidePolicy HidePolicy => this is IParametersOwner
      ? MemberHidePolicy.HIDE_BY_SIGNATURE
      : MemberHidePolicy.HIDE_BY_NAME;

    // todo
    public bool CanBeImplicitImplementation => false;
    public bool IsExplicitImplementation => false;
    public IList<IExplicitImplementation> ExplicitImplementations => EmptyList<IExplicitImplementation>.Instance;

    public virtual bool IsVisibleFromFSharp => true;
    public virtual bool CanNavigateTo => IsVisibleFromFSharp;

    public virtual bool IsExtensionMember => false;
    public abstract bool IsFSharpMember { get; }

    public virtual IList<ITypeParameter> AllTypeParameters => ContainingType.GetAllTypeParameters().ResultingList().Reverse();

    [CanBeNull]
    protected virtual FSharpSymbol GetActualSymbol([NotNull] FSharpSymbol symbol) => symbol;

    public FSharpSymbol Symbol
    {
      get
      {
        var declaration = GetDeclaration();
        var symbol = declaration?.GetFSharpSymbol();

        return symbol != null
          ? GetActualSymbol(symbol)
          : null;
      }
    }

    protected IType GetType([CanBeNull] FSharpType fsType) =>
      fsType != null
        ? fsType.MapType(AllTypeParameters, Module)
        : TypeFactory.CreateUnknownType(Module);
  }
}
