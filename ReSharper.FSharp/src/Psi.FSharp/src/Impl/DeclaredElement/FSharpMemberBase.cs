using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  internal abstract class FSharpMemberBase<TDeclaration> : FSharpTypeMember<TDeclaration>, IParametersOwner,
    IOverridableMember
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    [NotNull]
    public FSharpMemberOrFunctionOrValue FSharpSymbol { get; }

    protected FSharpMemberBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration)
    {
      FSharpSymbol = mfv;
    }


    public override IList<IAttributeInstance> GetAttributeInstances(bool inherit)
    {
      var attrs = new List<IAttributeInstance>();
      foreach (var attr in FSharpSymbol.Attributes)
        attrs.Add(new FSharpAttributeInstance(attr, Module));
      return attrs;
    }

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, bool inherit)
    {
      var attrs = new List<IAttributeInstance>();
      foreach (var attr in FSharpSymbol.Attributes)
        if (attr.AttributeType.FullName == clrName.FullName)
          attrs.Add(new FSharpAttributeInstance(attr, Module));
      return attrs;
    }

    public override bool HasAttributeInstance(IClrTypeName clrName, bool inherit)
    {
      return FSharpSymbol.Attributes.Any(a => a.AttributeType.QualifiedName.SubstringBefore(",") == clrName.FullName);
    }

    public InvocableSignature GetSignature(ISubstitution substitution)
    {
      return new InvocableSignature(this, substitution);
    }

    public override bool Equals(object obj)
    {
      return (obj as FSharpMemberBase<TDeclaration>)?.FSharpSymbol.IsEffectivelySameAs(FSharpSymbol) ?? false;
    }

    public override int GetHashCode()
    {
      return ShortName.GetHashCode();
    }

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations()
    {
      return EmptyList<IParametersOwnerDeclaration>.Instance;
    }

    public virtual IList<IParameter> Parameters => EmptyList<IParameter>.Instance;
    public bool IsRefReturn => false;
    public abstract override string ShortName { get; }
    public abstract IType ReturnType { get; }

    public override AccessRights GetAccessRights()
    {
      var accessibility = FSharpSymbol.Accessibility;
      if (accessibility.IsInternal)
        return AccessRights.INTERNAL;
      if (accessibility.IsPrivate)
        return AccessRights.PRIVATE;
      return AccessRights.PUBLIC;
    }

    public override bool IsStatic => !FSharpSymbol.IsInstanceMember;

    // todo: check interface impl
    public override bool IsOverride => FSharpSymbol.IsOverrideOrExplicitInterfaceImplementation;

    public override bool IsAbstract => FSharpSymbol.IsDispatchSlot;
    public override bool IsVirtual => false; // todo
    public bool IsExplicitImplementation => false;
    public IList<IExplicitImplementation> ExplicitImplementations => EmptyList<IExplicitImplementation>.Instance;
    public bool CanBeImplicitImplementation => true; // todo: set false and calc proper base element
  }
}