using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpDelegate([NotNull] IFSharpDelegatePart part)
    : Delegate(part), IFSharpSourceTypeElement, IFSharpTypeParametersOwner
  {
    public int MeasureTypeParametersCount { get; } = part.MeasureTypeParametersCount;

    protected override bool AcceptsPart(TypePart part) =>
      part.ShortName == ShortName &&
      part is IFSharpDelegatePart delegatePart && delegatePart.MeasureTypeParametersCount == MeasureTypeParametersCount;

    public string SourceName => this.GetSourceName();

    public IList<ITypeParameter> AllTypeParameters =>
      this.GetAllTypeParametersReversed();

    public override XmlNode GetXMLDoc(bool inherit) => this.GetXmlDoc(inherit);

    public ModuleMembersAccessKind AccessKind => ModuleMembersAccessKind.Normal;
    public ITypeDeclaration DefiningDeclaration => this.GetDefiningDeclaration();

    public override string ToString() => this.TestToString(BuildTypeParameterString());
  }
}
