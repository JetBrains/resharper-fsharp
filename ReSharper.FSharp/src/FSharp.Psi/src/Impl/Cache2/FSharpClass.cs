﻿using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpClass : Class, IFSharpTypeElement, IFSharpTypeParametersOwner
  {
    public int MeasureTypeParametersCount { get; }

    public FSharpClass([NotNull] IClassPart part) : base(part)
    {
      if (part is IFSharpClassPart fsClassPart)
        MeasureTypeParametersCount = fsClassPart.MeasureTypeParametersCount;
    }

    protected override bool AcceptsPart(TypePart part) =>
      part.ShortName == ShortName &&
      part is IFSharpClassPart classPart && classPart.MeasureTypeParametersCount == MeasureTypeParametersCount;

    protected override MemberDecoration Modifiers => myParts.GetModifiers();
    public virtual string SourceName => this.GetSourceName();

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

    public virtual IList<ITypeParameter> AllTypeParameters =>
      this.GetAllTypeParametersReversed();

    public new virtual XmlNode GetXMLDoc(bool inherit) => base.GetXMLDoc(inherit);
  }
}
