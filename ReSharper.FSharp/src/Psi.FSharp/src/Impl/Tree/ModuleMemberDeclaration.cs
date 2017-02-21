using System.Xml;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  // temp class for complementing generated PSI
  internal partial class OtherMemberDeclaration
  {
    public XmlNode GetXMLDoc(bool inherit)
    {
      throw new System.NotImplementedException();
    }

    public void SetName(string name)
    {
      throw new System.NotImplementedException();
    }

    public TreeTextRange GetNameRange()
    {
      throw new System.NotImplementedException();
    }

    public bool IsSynthetic()
    {
      throw new System.NotImplementedException();
    }

    public IDeclaredElement DeclaredElement { get; }
    public string DeclaredName { get; }
    public FSharpSymbol Symbol { get; set; }
  }
}