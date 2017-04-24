using System.Xml;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  // temp class for complementing generated PSI
  internal partial class OtherMemberDeclaration
  {
    public XmlNode GetXMLDoc(bool inherit)
    {
      return null;
    }

    public void SetName(string name)
    {
    }

    public TreeTextRange GetNameRange()
    {
      return TreeTextRange.InvalidRange;
    }

    public bool IsSynthetic()
    {
      return false;
    }

    public IDeclaredElement DeclaredElement => null;
    public string DeclaredName => SharedImplUtil.MISSING_DECLARATION_NAME;
    public string ShortName => DeclaredName;
    public string SourceName => FSharpImplUtil.GetSourceName(Identifier);
    public FSharpSymbol GetFSharpSymbol()
    {
      return null;
    }

    public FSharpSymbol Symbol { get; set; }
  }
}