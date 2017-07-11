using System.Xml;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  public abstract class FSharpDeclarationBase : FSharpCompositeElement, IFSharpDeclaration
  {
    public FSharpSymbol Symbol { get; set; }
    public virtual string ShortName => DeclaredName;
    public virtual string SourceName => DeclaredName;

    public virtual FSharpSymbol GetFSharpSymbol()
    {
      return (this.GetContainingFile() as IFSharpFile)?.GetSymbolDeclaration(GetNameRange().StartOffset.Offset);
    }

    public abstract IDeclaredElement DeclaredElement { get; }
    public abstract string DeclaredName { get; }
    public abstract TreeTextRange GetNameRange();

    public XmlNode GetXMLDoc(bool inherit)
    {
      return null; // todo
    }

    public void SetName(string name)
    {
      // todo
    }

    public bool IsSynthetic()
    {
      return false;
    }
  }
}