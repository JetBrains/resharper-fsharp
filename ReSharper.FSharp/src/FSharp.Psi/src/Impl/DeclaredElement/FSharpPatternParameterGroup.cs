using System.Collections.Generic;
using System.Xml;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpPatternParameterGroup : CachedTypeMemberBase, ITypeMember, IFSharpParameterDeclarationGroup
  {
    internal FSharpPatternParameterGroup([NotNull] IParametersPatternDeclaration declaration) : base(declaration)
    {
    }

    protected override bool CanBindTo(IDeclaration declaration) => declaration is IParametersPatternDeclaration;

    public bool IsSynthetic() => true;
    public DeclaredElementType GetElementType() => CLRDeclaredElementType.PARAMETER;

    public XmlNode GetXMLDoc(bool inherit) => null;
    public XmlNode GetXMLDescriptionSummary(bool inherit) => null;

    public string ShortName => SharedImplUtil.MISSING_DECLARATION_NAME;
    public bool CaseSensitiveName => true;

    public IFSharpParameterDeclaration GetParameterDeclaration(int index)
    {
      if (GetDeclaration() is not IParametersPatternDeclaration paramDecl)
        return null;

      var decls = paramDecl.ParameterDeclarations;

      if (decls.Count == 1)
        return decls[0];

      if (decls.Count <= index)
        return null;

      return decls[index];
    }

    public IList<IFSharpParameterDeclaration> ParameterDeclarations =>
      GetDeclaration() is IParametersPatternDeclaration paramDecl
        ? paramDecl.ParameterDeclarations
        : EmptyList<IFSharpParameterDeclaration>.Instance;

    public IList<IFSharpParameter> GetOrCreateParameters(IList<FSharpParameter> fcsParams)
    {
      if (GetDeclaration() is not IParametersPatternDeclaration paramsPatternDecl)
        return null;

      var paramDecls = paramsPatternDecl.ParameterDeclarations;
      var paramsGenerated = paramDecls.Count == 1 && fcsParams.Count != 1;

      var result = new List<IFSharpParameter>();
      foreach (var _ in fcsParams)
        result.Add(new FSharpPatternParameter(paramsGenerated));

      return result;
    }

    public ITypeMember GetContainingTypeMember() => null;
    public ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;

    public IList<IAttributeInstance> GetAttributeInstances(AttributesSource attributesSource) =>
      EmptyList<IAttributeInstance>.Instance;

    public IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, AttributesSource attributesSource) =>
      EmptyList<IAttributeInstance>.Instance;

    public bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource) => false;
    public AccessRights GetAccessRights() => AccessRights.NONE;

    public bool IsAbstract => false;
    public bool IsSealed => false;
    public bool IsVirtual => false;
    public bool IsOverride => false;
    public bool IsStatic => false;
    public bool IsReadonly => false;
    public bool IsExtern => false;
    public bool IsUnsafe => false;
    public bool IsVolatile => false;

    public string XMLDocId => "";
    public IList<TypeMemberInstance> GetHiddenMembers() => EmptyList<TypeMemberInstance>.Instance;
    public MemberHidePolicy HidePolicy => MemberHidePolicy.HIDE_BY_NAME;
    public AccessibilityDomain AccessibilityDomain => new(AccessibilityDomain.AccessibilityDomainType.PUBLIC, null);
  }
}
