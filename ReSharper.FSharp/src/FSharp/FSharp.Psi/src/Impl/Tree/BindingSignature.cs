using System;
using System.Collections.Generic;
using System.Xml;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class BindingSignatureStub
  {
    public bool IsMutable => MutableKeyword != null;

    public void SetIsMutable(bool value)
    {
      if (!value)
      {
        if (MutableKeyword != null)
        {
          ModificationUtil.DeleteChild(MutableKeyword);
        }
        return;
      }

      if (HeadPattern is { } headPat)
        headPat.AddTokenBefore(FSharpTokenType.MUTABLE);
    }

    public IFSharpParameterDeclaration GetParameterDeclaration(FSharpParameterIndex index) =>
      TypeUsage.GetParameterDeclaration(index);

    public IList<IList<IFSharpParameterDeclaration>> GetParameterDeclarations() =>
      TypeUsage.GetParameterDeclarations();

    TreeTextRange IDeclaration.GetNameRange() => TreeTextRange.InvalidRange;

    FSharpSymbol IFSharpDeclaration.GetFcsSymbol() => throw new InvalidOperationException();
    FSharpSymbolUse IFSharpDeclaration.GetFcsSymbolUse() => throw new InvalidOperationException();
    string IDeclaration.DeclaredName => throw new InvalidOperationException();
    void IDeclaration.SetName(string name) => throw new InvalidOperationException();
    bool IDeclaration.IsSynthetic() => throw new InvalidOperationException();

    string IFSharpDeclaration.SourceName => throw new InvalidOperationException();
    string IFSharpDeclaration.CompiledName => throw new InvalidOperationException();
    void IFSharpDeclaration.SetName(string name, ChangeNameKind changeNameKind) => throw new InvalidOperationException();
    TreeTextRange IFSharpDeclaration.GetNameIdentifierRange() => throw new InvalidOperationException();
    XmlDocBlock IFSharpDeclaration.XmlDocBlock => throw new InvalidOperationException();
    IFSharpIdentifier INameIdentifierOwner.NameIdentifier => throw new InvalidOperationException();

    XmlNode IXmlDocOwnerTreeNode.GetXMLDoc(bool inherit) => throw new InvalidOperationException();
    IDeclaredElement IDeclaration.DeclaredElement => throw new InvalidOperationException();
  }

  internal class BindingSignature : BindingSignatureStub
  {
    public override ITypeUsage SetTypeUsage(ITypeUsage typeUsage)
    {
      if (TypeUsage != null)
        return base.SetTypeUsage(typeUsage);

      var colon = ModificationUtil.AddChildAfter(HeadPattern, FSharpTokenType.COLON.CreateTreeElement());
      return ModificationUtil.AddChildAfter(colon, typeUsage);
    }
  }
}
