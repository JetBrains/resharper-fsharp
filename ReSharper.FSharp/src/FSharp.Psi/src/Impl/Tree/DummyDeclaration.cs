using System.Xml;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class DummyDeclaration : FSharpCompositeElement, IFSharpDeclaration
  {
    public XmlNode GetXMLDoc(bool inherit) => null;

    public void SetName(string name)
    {
    }

    public void SetName(string name, ChangeNameKind changeNameKind)
    {
    }

    public TreeTextRange GetNameRange() => TreeTextRange.InvalidRange;
    public bool IsSynthetic() => false;
    public IDeclaredElement DeclaredElement => null;
    public string DeclaredName => SharedImplUtil.MISSING_DECLARATION_NAME;
    public string ShortName => DeclaredName;
    public string SourceName => SharedImplUtil.MISSING_DECLARATION_NAME;
    public FSharpSymbol GetFSharpSymbol() => null;
    public FSharpSymbol Symbol { get; set; }
  }
}