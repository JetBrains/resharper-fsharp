﻿using System;
using System.Xml;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public abstract class FSharpDeclarationBase : FSharpCompositeElement, IFSharpDeclaration
  {
    public abstract string CompiledName { get; }
    public virtual string SourceName => NameIdentifier.GetSourceName();

    public virtual FSharpSymbol GetFcsSymbol() =>
      GetFcsSymbolUse()?.Symbol;

    public FSharpSymbolUse GetFcsSymbolUse() =>
      GetSymbolDeclaration(GetNameIdentifierRange());

    protected virtual FSharpSymbolUse GetSymbolDeclaration(TreeTextRange identifierRange) =>
      FSharpFile.GetSymbolDeclaration(identifierRange.StartOffset.Offset);

    public abstract IDeclaredElement DeclaredElement { get; }
    public virtual string DeclaredName => CompiledName;

    public abstract IFSharpIdentifier NameIdentifier { get; }
    public virtual TreeTextRange GetNameRange() => NameIdentifier.GetNameRange();
    public virtual TreeTextRange GetNameIdentifierRange() => NameIdentifier.GetNameIdentifierRange();

    public virtual XmlDocBlock XmlDocBlock => FirstChild as XmlDocBlock;
    public XmlNode GetXMLDoc(bool inherit) => XmlDocBlock?.GetXML(null);

    public bool IsSynthetic() => false;

    public virtual void SetName(string name) =>
      throw new InvalidOperationException("Use IFSharpDeclaration.SetName(string, ChangeNameKind)");

    public virtual void SetName(string name, ChangeNameKind changeNameKind) =>
      NameIdentifier.ReplaceIdentifier(name);
  }
}
