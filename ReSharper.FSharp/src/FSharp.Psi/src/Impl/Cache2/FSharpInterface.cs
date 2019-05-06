using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpInterface : Interface, IFSharpTypeElement
  {
    public FSharpInterface(IInterfacePart part) : base(part)
    {
    }

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
  }
}
