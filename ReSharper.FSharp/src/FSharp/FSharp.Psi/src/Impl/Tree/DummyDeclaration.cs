using System.Xml;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;

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

    public IFSharpIdentifier NameIdentifier => null;
    public TreeTextRange GetNameRange() => TreeTextRange.InvalidRange;
    public TreeTextRange GetNameIdentifierRange() => TreeTextRange.InvalidRange;
    public XmlDocBlock XmlDocBlock => null;

    public bool IsSynthetic() => false;
    public IDeclaredElement DeclaredElement => null;
    public string DeclaredName => SharedImplUtil.MISSING_DECLARATION_NAME;
    public string CompiledName => DeclaredName;
    public string SourceName => DeclaredName;
    public FSharpSymbol GetFcsSymbol() => null;
    public FSharpSymbolUse GetFcsSymbolUse() => null;
    public FSharpSymbol Symbol { get; set; }
  }
}