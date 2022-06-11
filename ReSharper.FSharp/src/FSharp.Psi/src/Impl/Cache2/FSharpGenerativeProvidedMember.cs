using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public abstract class FSharpGenerativeProvidedElement<T> : FSharpGeneratedMemberBase
    where T : IProvidedCustomAttributeProvider
  {
    protected readonly ITypeElement ContainingTypeElement;

    protected FSharpGenerativeProvidedElement(T info, ITypeElement containingTypeElement)
    {
      ContainingTypeElement = containingTypeElement;
      Info = info;
    }

    protected T Info { get; }
    public override IPsiModule Module => ContainingTypeElement.Module;
    public override string XMLDocId => XMLDocUtil.GetTypeMemberXmlDocId(this, ShortName);
    public abstract override string ShortName { get; }
    protected override IClrDeclaredElement ContainingElement => ContainingTypeElement;
    public override ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;
    public override XmlNode GetXMLDoc(bool inherit) => Info.GetXmlDoc(this);
    public override ITypeMember GetContainingTypeMember() => ContainingType as ITypeMember;
    public override ITypeElement GetContainingType() => ContainingTypeElement;

    public override bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource) =>
      Info?.GetAttributeConstructorArgs(null, clrName.FullName) != null;

    public override IList<IAttributeInstance> GetAttributeInstances(AttributesSource attributesSource) =>
      Info is IRdProvidedCustomAttributesOwner x
        ? x.Attributes.Select(t => (IAttributeInstance)new FSharpProvidedAttributeInstance(t, Module)).ToList()
        : EmptyList<IAttributeInstance>.Instance;

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName,
      AttributesSource attributesSource) =>
      GetAttributeInstances(attributesSource).Where(t => t.GetClrName().Equals(clrName)).ToList();
  }


  public abstract class FSharpGenerativeProvidedMember<T> : FSharpGenerativeProvidedElement<T>, IOverridableMember,
    ISecondaryDeclaredElement where T : ProvidedMemberInfo
  {
    private IClrDeclaredElement myOriginElement;

    protected FSharpGenerativeProvidedMember(T info, ITypeElement containingType) : base(info, containingType)
    {
    }

    public override string ShortName => Info.Name;
    public IClrDeclaredElement OriginElement => myOriginElement ??= GetOriginElement();
    public bool IsReadOnly => true;

    private IClrDeclaredElement GetOriginElement()
    {
      var containingType = ContainingTypeElement;
      while (containingType is FSharpGenerativeProvidedNestedClass)
        containingType = containingType.GetContainingType();

      return containingType;
    }
  }
}
