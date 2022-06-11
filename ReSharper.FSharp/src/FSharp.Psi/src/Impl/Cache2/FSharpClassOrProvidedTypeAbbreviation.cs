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
    private GenerativeMembersConverter<FSharpClassOrProvidedTypeAbbreviation> Class =>
      IsProvidedAndGenerated &&
      ProvidedTypesResolveUtil.TryGetProvidedType(Module, GetClrName(), out var type)
        ? new GenerativeMembersConverter<FSharpClassOrProvidedTypeAbbreviation>(type, this)
        : null;

    public bool IsProvidedAndGenerated => myParts is TypeAbbreviationOrDeclarationPart { IsProvidedAndGenerated: true };

    public FSharpClassOrProvidedTypeAbbreviation([NotNull] IClassPart part) : base(part)
    {
    }

    public override MemberPresenceFlag GetMemberPresenceFlag() =>
      Class is { } x ? x.GetMemberPresenceFlag() : base.GetMemberPresenceFlag();

    public override IClass GetSuperClass() => Class is { } x ? x.GetSuperClass() : base.GetSuperClass();

    public override IList<ITypeElement> GetSuperTypeElements() =>
      Class is { } x ? x.GetSuperTypeElements() : base.GetSuperTypeElements();

    public override IEnumerable<ITypeMember> GetMembers() =>
      Class is { } x ? x.GetMembers() : base.GetMembers();

    public override IList<ITypeElement> NestedTypes => Class is { } x ? x.NestedTypes : base.NestedTypes;

    public override IList<ITypeParameter> AllTypeParameters =>
      Class is { } x ? x.AllTypeParameters : base.AllTypeParameters;

    public override bool HasMemberWithName(string shortName, bool ignoreCase) =>
      Class is { } x
        ? x.HasMemberWithName(shortName, ignoreCase)
        : base.HasMemberWithName(shortName, ignoreCase);

    public override IEnumerable<IConstructor> Constructors =>
      Class is { } x ? x.Constructors : base.Constructors;

    public override IEnumerable<IOperator> Operators => Class is { } x ? x.Operators : base.Operators;
    public override IEnumerable<IMethod> Methods => Class is { } x ? x.Methods : base.Methods;
    public override IEnumerable<IProperty> Properties => Class is { } x ? x.Properties : base.Properties;
    public override IEnumerable<IEvent> Events => Class is { } x ? x.Events : base.Events;
    public override IEnumerable<string> MemberNames => Class is { } x ? x.MemberNames : base.MemberNames;
    public override IEnumerable<IField> Constants => Class is { } x ? x.Constants : base.Constants;
    public override IEnumerable<IField> Fields => Class is { } x ? x.Fields : base.Fields;

    public override XmlNode GetXMLDoc(bool inherit) =>
      Class is { } x ? x.GetXmlDoc() : base.GetXMLDoc(inherit);
  }
}
