using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpDelegate : Delegate, IFSharpTypeElement, IFSharpTypeParametersOwner
  {
    public int MeasureTypeParametersCount { get; }

    public FSharpDelegate([NotNull] IFSharpDelegatePart part) : base(part) =>
      MeasureTypeParametersCount = part.MeasureTypeParametersCount;

    protected override bool AcceptsPart(TypePart part) =>
      part.ShortName == ShortName &&
      part is IFSharpDelegatePart delegatePart && delegatePart.MeasureTypeParametersCount == MeasureTypeParametersCount;

    public string SourceName => this.GetSourceName();

    public IList<ITypeParameter> AllTypeParameters =>
      this.GetAllTypeParametersReversed();

    public override XmlNode GetXMLDoc(bool inherit) => this.GetXmlDoc(inherit);
  }
}
