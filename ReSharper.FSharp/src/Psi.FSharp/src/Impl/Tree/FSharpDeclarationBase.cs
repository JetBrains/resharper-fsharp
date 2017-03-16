using System.Xml;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  public abstract class FSharpDeclarationBase : FSharpCompositeElement, IFSharpDeclaration, IDeclaration
  {
    public FSharpSymbol Symbol { get; set; }
    public virtual string ShortName => DeclaredName;

    public abstract IDeclaredElement DeclaredElement { get; }
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
  }
}