using System.Collections.Generic;
using System.Xml;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpInterface(IFSharpInterfacePart part)
    : Interface(part), IFSharpSourceTypeElement, IFSharpTypeParametersOwner
  {
    public int MeasureTypeParametersCount { get; } = part.MeasureTypeParametersCount;

    protected override bool AcceptsPart(TypePart part) =>
      part.ShortName == ShortName &&
      part is IFSharpInterfacePart interfacePart && interfacePart.MeasureTypeParametersCount == MeasureTypeParametersCount;

    protected override MemberDecoration Modifiers => myParts.GetModifiers();
    public string SourceName => this.GetSourceName();

    public override IList<ITypeElement> GetSuperTypeElements()
    {
      var result = new HashSet<ITypeElement>();
      foreach (var part in EnumerateParts())
        if (part is IFSharpClassLikePart fsPart)
          result.AddRange(fsPart.GetSuperTypeElements());
      return result.ToArray();
    }

    public IList<ITypeParameter> AllTypeParameters =>
      this.GetAllTypeParametersReversed();

    public override XmlNode GetXMLDoc(bool inherit) => this.GetXmlDoc(inherit);

    public ModuleMembersAccessKind AccessKind => ModuleMembersAccessKind.Normal;
    public ITypeDeclaration DefiningDeclaration => this.GetDefiningDeclaration();

    public override string ToString() => this.TestToString(BuildTypeParameterString());
  }
}
