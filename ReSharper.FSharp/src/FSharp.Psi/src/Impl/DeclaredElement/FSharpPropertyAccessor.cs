using System.Collections.Generic;
using System.Xml;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  public class FSharpPropertyAccessor : IAccessor, IFSharpTypeParametersOwner
  {
    private readonly FSharpMemberOrFunctionOrValue myMfv;
    private readonly ImplicitAccessor myImplicitAccessor;

    public FSharpPropertyAccessor(FSharpMemberOrFunctionOrValue mfv, IOverridableMember owner, AccessorKind kind)
    {
      myMfv = mfv;
      myImplicitAccessor = new ImplicitAccessor(owner, kind);
    }

    public InvocableSignature GetSignature(ISubstitution substitution) => myImplicitAccessor.GetSignature(substitution);

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations() =>
      myImplicitAccessor.GetParametersOwnerDeclarations();

    public IList<IParameter> Parameters => this.GetParameters(myMfv);
    public IType ReturnType => myImplicitAccessor.ReturnType;
    public ReferenceKind ReturnKind => myImplicitAccessor.ReturnKind;
    public AccessRights GetAccessRights() => myMfv.GetAccessRights();
    public string SourceName => SharedImplUtil.MISSING_DECLARATION_NAME;
    public IList<ITypeParameter> AllTypeParameters => GetContainingType().GetAllTypeParametersReversed();

    public DeclaredElementType GetElementType() => myImplicitAccessor.GetElementType();

    public bool IsValid()
    {
      if (!(OwnerMember is IProperty ownerMember) || !ownerMember.IsValid()) return false;
      return Kind switch
      {
        AccessorKind.GETTER => Equals(ownerMember.Getter, this),
        AccessorKind.SETTER => Equals(ownerMember.Setter, this),
        _ => false
      };
    }

    public bool IsSynthetic() => myImplicitAccessor.IsSynthetic();

    public IList<IDeclaration> GetDeclarations() => myImplicitAccessor.GetDeclarations();

    public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile) =>
      myImplicitAccessor.GetDeclarationsIn(sourceFile);

    public HybridCollection<IPsiSourceFile> GetSourceFiles() => myImplicitAccessor.GetSourceFiles();

    public bool HasDeclarationsIn(IPsiSourceFile sourceFile) => myImplicitAccessor.HasDeclarationsIn(sourceFile);

    public IPsiServices GetPsiServices() => myImplicitAccessor.GetPsiServices();

    public XmlNode GetXMLDoc(bool inherit) => myImplicitAccessor.GetXMLDoc(inherit);

    public XmlNode GetXMLDescriptionSummary(bool inherit) => myImplicitAccessor.GetXMLDescriptionSummary(inherit);

    public string ShortName => myImplicitAccessor.ShortName;
    public bool CaseSensitiveName => myImplicitAccessor.CaseSensitiveName;
    public PsiLanguageType PresentationLanguage => myImplicitAccessor.PresentationLanguage;

    public ITypeElement GetContainingType() => myImplicitAccessor.GetContainingType();

    public ITypeMember GetContainingTypeMember() => myImplicitAccessor.GetContainingTypeMember();

    public IPsiModule Module => myImplicitAccessor.Module;
    public ISubstitution IdSubstitution => myImplicitAccessor.IdSubstitution;

    public IList<IAttributeInstance> GetAttributeInstances(AttributesSource attributesSource) =>
      myImplicitAccessor.GetAttributeInstances(attributesSource);

    public IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, AttributesSource attributesSource) =>
      myImplicitAccessor.GetAttributeInstances(clrName, attributesSource);

    public bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource) =>
      myImplicitAccessor.HasAttributeInstance(clrName, attributesSource);

    public IAttributesSet ReturnTypeAttributes => myImplicitAccessor.ReturnTypeAttributes;
    public bool IsAbstract => myImplicitAccessor.IsAbstract;
    public bool IsSealed => myImplicitAccessor.IsSealed;
    public bool IsVirtual => myImplicitAccessor.IsVirtual;
    public bool IsOverride => myImplicitAccessor.IsOverride;
    public bool IsStatic => myImplicitAccessor.IsStatic;
    public bool IsReadonly => myImplicitAccessor.IsReadonly;
    public bool IsExtern => myImplicitAccessor.IsExtern;
    public bool IsUnsafe => myImplicitAccessor.IsUnsafe;
    public bool IsVolatile => myImplicitAccessor.IsVolatile;
    public string XMLDocId => myImplicitAccessor.XMLDocId;

    public IList<TypeMemberInstance> GetHiddenMembers() => myImplicitAccessor.GetHiddenMembers();

    public Hash? CalcHash() => myImplicitAccessor.CalcHash();

    public AccessibilityDomain AccessibilityDomain => myImplicitAccessor.AccessibilityDomain;
    public MemberHidePolicy HidePolicy => myImplicitAccessor.HidePolicy;
    public bool IsPredefined => myImplicitAccessor.IsPredefined;
    public bool IsIterator => myImplicitAccessor.IsIterator;
    public bool IsExplicitImplementation => myImplicitAccessor.IsExplicitImplementation;
    public IList<IExplicitImplementation> ExplicitImplementations => myImplicitAccessor.ExplicitImplementations;
    public bool CanBeImplicitImplementation => myImplicitAccessor.CanBeImplicitImplementation;
    public IList<ITypeParameter> TypeParameters => myImplicitAccessor.TypeParameters;
    public bool IsExtensionMethod => myImplicitAccessor.IsExtensionMethod;
    public bool IsAsync => myImplicitAccessor.IsAsync;
    public bool IsVarArg => myImplicitAccessor.IsVarArg;
    public IOverridableMember OwnerMember => myImplicitAccessor.OwnerMember;
    public AccessorKind Kind => myImplicitAccessor.Kind;
    public bool IsInitOnly => myImplicitAccessor.IsInitOnly;
    public IParameter ValueVariable => myImplicitAccessor.ValueVariable;

    public override bool Equals(object obj) =>
      obj is FSharpPropertyAccessor accessor && XMLDocId == accessor.XMLDocId;

    public override int GetHashCode() => XMLDocId.GetHashCode();
  }
}
