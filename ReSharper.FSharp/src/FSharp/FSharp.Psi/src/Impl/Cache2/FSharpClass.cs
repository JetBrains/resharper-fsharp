using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpClass : Class, IFSharpSourceTypeElement, IFSharpTypeParametersOwner
  {
    public int MeasureTypeParametersCount { get; }

    public FSharpClass([NotNull] IClassPart part) : base(part)
    {
      if (part is IFSharpClassPart fsClassPart)
        MeasureTypeParametersCount = fsClassPart.MeasureTypeParametersCount;
    }

    // todo: check the same file
    protected override bool AcceptsPart(TypePart part) =>
      // todo: make UnionPart/RecordPart implement IFSharpClassPart, simplify this check
      part.ShortName == ShortName &&
      part is IClassPart and IFSharpTypePart typePart and not IModulePart && 
      typePart.MeasureTypeParametersCount == MeasureTypeParametersCount;

    protected override MemberDecoration Modifiers => myParts.GetModifiers();
    public string SourceName => this.GetSourceName();

    public virtual ModuleMembersAccessKind AccessKind => EnumerateParts().GetAccessKind();
    public ITypeDeclaration DefiningDeclaration => this.GetDefiningDeclaration();

    public override IClass GetSuperClass()
    {
      foreach (var part in EnumerateParts())
        if (part is IFSharpClassPart fsPart && fsPart.GetSuperClass() is { } super)
          return super;
      return null;
    }

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

    public override string ToString() => this.TestToString(BuildTypeParameterString());
  }
}
