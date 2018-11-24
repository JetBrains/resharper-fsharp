using System.Xml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  // temp class for complementing generated PSI
  internal partial class OtherMemberDeclaration
  {
    public XmlNode GetXMLDoc(bool inherit) => null;

    public void SetName(string name)
    {
    }

    public TreeTextRange GetNameRange() => TreeTextRange.InvalidRange;
    public bool IsSynthetic() => false;
    public IDeclaredElement DeclaredElement => null;
    public string DeclaredName => SharedImplUtil.MISSING_DECLARATION_NAME;
    public string ShortName => DeclaredName;
    public string SourceName => Identifier.GetSourceName();
    public FSharpSymbol GetFSharpSymbol() => null;
    public FSharpSymbol Symbol { get; set; }
  }
}