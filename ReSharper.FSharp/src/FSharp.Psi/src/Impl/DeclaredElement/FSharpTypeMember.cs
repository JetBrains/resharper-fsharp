﻿using System.Collections.Generic;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.dataStructures;
using JetBrains.Util.DataStructures;

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

      using var _ = CompilationContextCookie.GetOrCreate(containingType.Module.GetContextFromModule());

      var list = new FrugalLocalList<IDeclaration>();
      var typeDeclarations =
        sourceFile != null
          ? containingType.GetDeclarationsIn(sourceFile)
          : containingType.GetDeclarations();

      foreach (var partDeclaration in typeDeclarations)
      {
        if (!(partDeclaration is IFSharpTypeElementDeclaration typeDeclaration))
          continue;

        foreach (var memberDecl in typeDeclaration.MemberDeclarations)
          if (memberDecl.DeclaredName == declaration.DeclaredName && Equals(this, memberDecl.DeclaredElement))
            list.Add(memberDecl);
      }

      return list.ResultingList();
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

      if (!(obj is IFSharpMember member)) return false;

      if (!ShortName.Equals(member.ShortName))
        return false;

      return Equals(GetContainingType(), member.GetContainingType());
    }

    public override int GetHashCode() => ShortName.GetHashCode();

    public virtual bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource) => false;

    public virtual IList<IAttributeInstance> GetAttributeInstances(AttributesSource attributesSource) =>
      EmptyList<IAttributeInstance>.Instance;

    public virtual IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, AttributesSource attributesSource) =>
      EmptyList<IAttributeInstance>.Instance;

    public virtual AccessRights GetAccessRights() =>
      GetDeclaration()?.GetAccessRights() ?? AccessRights.PUBLIC;

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

    public AccessibilityDomain AccessibilityDomain => new(AccessibilityDomain.AccessibilityDomainType.PUBLIC, null);

    public MemberHidePolicy HidePolicy => this is IParametersOwner
      ? MemberHidePolicy.HIDE_BY_SIGNATURE
      : MemberHidePolicy.HIDE_BY_NAME;

    public virtual bool IsVisibleFromFSharp => true;
    public virtual bool CanNavigateTo => IsVisibleFromFSharp;

    public virtual IList<ITypeParameter> AllTypeParameters =>
      GetContainingType().GetAllTypeParameters().ResultingList().Reverse();

    [CanBeNull]
    protected virtual FSharpSymbol GetActualSymbol([NotNull] FSharpSymbol symbol) => symbol;

    public FSharpSymbol Symbol
    {
      get
      {
        var declaration = GetDeclaration();
        var symbol = declaration?.GetFcsSymbol();

        return symbol != null
          ? GetActualSymbol(symbol)
          : null;
      }
    }

    public FSharpSymbolUse SymbolUse =>
      GetDeclaration()?.GetFcsSymbolUse();

    [NotNull]
    protected IType GetType([CanBeNull] FSharpType fcsType) =>
      fcsType != null
        ? fcsType.MapType(AllTypeParameters, Module)
        : TypeFactory.CreateUnknownType(Module);
  }
}
