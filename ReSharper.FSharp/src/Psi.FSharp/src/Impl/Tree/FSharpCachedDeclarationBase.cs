using System.Xml;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal abstract class FSharpCachedDeclarationBase : FSharpCompositeElement, ICachedDeclaration2
  {
    public abstract string DeclaredName { get; }
    public abstract void SetName(string name);
    public abstract TreeTextRange GetNameRange();

    public XmlNode GetXMLDoc(bool inherit)
    {
      return null; // todo
    }

    public bool IsSynthetic()
    {
      return false;
    }

    protected override void PreInit()
    {
      base.PreInit();
      CacheDeclaredElement = null;
    }

    public IDeclaredElement DeclaredElement
    {
      get
      {
        Assertion.Assert(IsValid(), "Getting declared element from invalid declaration");
        Assertion.Assert(CacheDeclaredElement == null || CacheDeclaredElement.IsValid(),
          "myCacheDeclaredElement == null || myCacheDeclaredElement.IsValid()");
        return CacheDeclaredElement;
      }
    }

    public IDeclaredElement CacheDeclaredElement { get; set; }
  }
}