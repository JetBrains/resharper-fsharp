using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpClass : Class, IFSharpTypeElement, IFSharpTypeParametersOwner
  {
    public FSharpClass([NotNull] IClassPart part) : base(part)
    {
    }

    protected override bool AcceptsPart(TypePart part) =>
      base.AcceptsPart(part) && !(part is IModulePart);

    protected override MemberDecoration Modifiers => myParts.GetModifiers();
    public string SourceName => this.GetSourceName();

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
  }
}
