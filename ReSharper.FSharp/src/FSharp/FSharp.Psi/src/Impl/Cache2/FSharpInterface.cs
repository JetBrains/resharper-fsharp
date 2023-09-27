using System.Collections.Generic;
using System.Xml;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpInterface : Interface, IFSharpTypeElement, IFSharpTypeParametersOwner
  {
    public int MeasureTypeParametersCount { get; }

    public FSharpInterface(IFSharpInterfacePart part) : base(part) =>
      MeasureTypeParametersCount = part.MeasureTypeParametersCount;

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
  }
}
