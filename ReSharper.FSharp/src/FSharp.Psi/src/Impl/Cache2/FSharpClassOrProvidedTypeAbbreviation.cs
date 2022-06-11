using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpClassOrProvidedTypeAbbreviation : FSharpClass
  {
    // Triggers FCS resolve
    private GenerativeMembersConverter<FSharpClassOrProvidedTypeAbbreviation> ProvidedClass =>
      IsProvidedAndGenerated &&
      ProvidedTypesResolveUtil.TryGetProvidedType(Module, GetClrName(), out var type)
        ? new GenerativeMembersConverter<FSharpClassOrProvidedTypeAbbreviation>(type, this)
        : null;

    public bool IsProvidedAndGenerated => myParts is TypeAbbreviationOrDeclarationPart { IsProvidedAndGenerated: true };

    public FSharpClassOrProvidedTypeAbbreviation([NotNull] IClassPart part) : base(part)
    {
    }

    public override MemberPresenceFlag GetMemberPresenceFlag() =>
      ProvidedClass is { } x ? x.GetMemberPresenceFlag() : base.GetMemberPresenceFlag();

    public override IClass GetSuperClass() => ProvidedClass is { } x ? x.GetSuperClass() : base.GetSuperClass();

    public override IList<ITypeElement> GetSuperTypeElements() =>
      ProvidedClass is { } x ? x.GetSuperTypeElements() : base.GetSuperTypeElements();

    public override IEnumerable<ITypeMember> GetMembers() =>
      ProvidedClass is { } x ? x.GetMembers() : base.GetMembers();

    public override IList<ITypeElement> NestedTypes => ProvidedClass is { } x ? x.NestedTypes : base.NestedTypes;

    public override IList<ITypeParameter> AllTypeParameters =>
      ProvidedClass is { } x ? x.AllTypeParameters : base.AllTypeParameters;

    public override bool HasMemberWithName(string shortName, bool ignoreCase) =>
      ProvidedClass is { } x
        ? x.HasMemberWithName(shortName, ignoreCase)
        : base.HasMemberWithName(shortName, ignoreCase);

    public override IEnumerable<IConstructor> Constructors =>
      ProvidedClass is { } x ? x.Constructors : base.Constructors;

    public override IEnumerable<IOperator> Operators => ProvidedClass is { } x ? x.Operators : base.Operators;
    public override IEnumerable<IMethod> Methods => ProvidedClass is { } x ? x.Methods : base.Methods;
    public override IEnumerable<IProperty> Properties => ProvidedClass is { } x ? x.Properties : base.Properties;
    public override IEnumerable<IEvent> Events => ProvidedClass is { } x ? x.Events : base.Events;
    public override IEnumerable<string> MemberNames => ProvidedClass is { } x ? x.MemberNames : base.MemberNames;
    public override IEnumerable<IField> Constants => ProvidedClass is { } x ? x.Constants : base.Constants;
    public override IEnumerable<IField> Fields => ProvidedClass is { } x ? x.Fields : base.Fields;

    public override XmlNode GetXMLDoc(bool inherit) =>
      ProvidedClass is { } x ? x.GetXmlDoc() : base.GetXMLDoc(inherit);
  }
}
