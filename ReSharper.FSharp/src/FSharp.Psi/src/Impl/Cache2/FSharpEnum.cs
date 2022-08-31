using System.Xml;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpEnum : Enum, IFSharpTypeElement
  {
    public int MeasureTypeParametersCount { get; }

    public FSharpEnum(IFSharpEnumPart part) : base(part) =>
      MeasureTypeParametersCount = part.MeasureTypeParametersCount;

    protected override bool AcceptsPart(TypePart part) =>
      part.ShortName == ShortName &&
      part is IFSharpEnumPart enumPart && enumPart.MeasureTypeParametersCount == MeasureTypeParametersCount;

    public string SourceName => this.GetSourceName();

    public override XmlNode GetXMLDoc(bool inherit) => this.GetXmlDoc(inherit);
  }
}
