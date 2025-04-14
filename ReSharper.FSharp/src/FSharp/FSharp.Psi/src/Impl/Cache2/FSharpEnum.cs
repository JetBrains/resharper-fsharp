using System.Xml;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpEnum(IFSharpEnumPart part) : Enum(part), IFSharpSourceTypeElement
  {
    public int MeasureTypeParametersCount { get; } = part.MeasureTypeParametersCount;

    protected override bool AcceptsPart(TypePart part) =>
      part.ShortName == ShortName &&
      part is IFSharpEnumPart enumPart && enumPart.MeasureTypeParametersCount == MeasureTypeParametersCount;

    public string SourceName => this.GetSourceName();

    public override XmlNode GetXMLDoc(bool inherit) => this.GetXmlDoc(inherit);
    
    public ModuleMembersAccessKind AccessKind => ModuleMembersAccessKind.RequiresQualifiedAccess;
    public ITypeDeclaration DefiningDeclaration => this.GetDefiningDeclaration();

    public override string ToString() => this.TestToString(BuildTypeParameterString());
  }
}
