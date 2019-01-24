using System;
using System.Xml;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public abstract class FSharpDeclarationBase : FSharpCompositeElement, IFSharpDeclaration
  {
    public virtual string ShortName => DeclaredName;
    public virtual string SourceName => NameIdentifier.GetSourceName();

    public virtual FSharpSymbol GetFSharpSymbol() =>
      (this.GetContainingFile() as IFSharpFile)?.GetSymbolDeclaration(GetNameIdentifierRange().StartOffset.Offset);

    public abstract IDeclaredElement DeclaredElement { get; }
    public abstract string DeclaredName { get; }

    public abstract IFSharpIdentifier NameIdentifier { get; }
    public virtual TreeTextRange GetNameRange() => NameIdentifier.GetNameRange();
    public virtual TreeTextRange GetNameIdentifierRange() => NameIdentifier.GetNameIdentifierRange();

    public XmlNode GetXMLDoc(bool inherit) => null;
    public bool IsSynthetic() => false;

    public virtual void SetName(string name) =>
      throw new InvalidOperationException("Use IFSharpDeclaration.SetName(string, ChangeNameKind)");

    public void SetName(string name, ChangeNameKind changeNameKind)
    {
    }
  }
}