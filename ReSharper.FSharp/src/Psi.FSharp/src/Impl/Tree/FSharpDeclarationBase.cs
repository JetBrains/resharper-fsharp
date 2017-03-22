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

    public virtual FSharpSymbol GetFSharpSymbol()
    {
      if (Symbol != null)
        return Symbol;

      var nameRange = GetNameRange();
      if (!nameRange.IsValid())
        return null;

      var fsFile = this.GetContainingFile() as IFSharpFile;
      Assertion.AssertNotNull(fsFile, "fsFile != null");
      var token = fsFile.FindTokenAt(nameRange.StartOffset);
      if (token == null)
        return null;

      return Symbol = FSharpSymbolsUtil.TryFindFSharpSymbol(fsFile, token.GetText(), nameRange.EndOffset.Offset);
    }

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