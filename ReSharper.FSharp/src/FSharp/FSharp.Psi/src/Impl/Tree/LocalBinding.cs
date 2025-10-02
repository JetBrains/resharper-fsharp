using System;
using System.Collections.Generic;
using System.Xml;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LocalBindingStub
  {
    public bool IsMutable => MutableKeyword != null;

    public void SetIsMutable(bool value)
    {
      if (!value)
        throw new NotImplementedException();

      var headPat = HeadPattern;
      if (headPat != null)
        headPat.AddTokenBefore(FSharpTokenType.MUTABLE);
    }

    public bool IsInline => InlineKeyword != null;

    public void SetIsInline(bool value)
    {
      if (value)
        throw new NotImplementedException();

      using var _ = WriteLockCookie.Create(IsPhysical());
      var inlineKeyword = InlineKeyword;
      if (inlineKeyword.PrevSibling is Whitespace whitespace)
        ModificationUtil.DeleteChildRange(whitespace, inlineKeyword);
      else
        ModificationUtil.DeleteChild(inlineKeyword);
    }


    public bool HasParameters => !ParametersDeclarationsEnumerable.IsEmpty();
    public bool IsLiteral => false;
    public bool IsComputed => LetOrUseExprNavigator.GetByBinding(this)?.IsComputed == true;

    IDeclaredElement IParameterOwnerMemberDeclaration.DeclaredElement =>
      HeadPattern is IReferencePat rp ? rp.DeclaredElement : null;

    FSharpSymbolUse IParameterOwnerMemberDeclaration.GetFcsSymbolUse() =>
      HeadPattern is IReferencePat rp ? rp.GetFcsSymbolUse() : null;

    IFSharpParameterDeclaration IFSharpParameterOwnerDeclaration.GetParameterDeclaration(FSharpParameterIndex index) =>
      this.GetBindingParameterPatterns().GetParameterDeclaration(index);

    IList<IList<IFSharpParameterDeclaration>> IFSharpParameterOwnerDeclaration.GetParameterDeclarations() =>
      this.GetBindingParameterDeclarations();

    public void SetParameterFcsType(FSharpParameterIndex index, FSharpType fcsType) =>
      this.GetBindingParameterPatterns().SetParameterFcsType(this, index, fcsType);

    [CanBeNull] private IReferencePat HeadReferencePat => HeadPattern as IReferencePat;

    string IFSharpDeclaration.SourceName =>
      HeadReferencePat?.SourceName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    IDeclaredElement IDeclaration.DeclaredElement => null;
    TreeTextRange IDeclaration.GetNameRange() => TreeTextRange.InvalidRange;

    FSharpSymbol IFSharpDeclaration.GetFcsSymbol() => HeadReferencePat?.GetFcsSymbol();
    FSharpSymbolUse IFSharpDeclaration.GetFcsSymbolUse() => HeadReferencePat?.GetFcsSymbolUse();
    string IDeclaration.DeclaredName => throw new InvalidOperationException();
    void IDeclaration.SetName(string name) => throw new InvalidOperationException();
    bool IDeclaration.IsSynthetic() => throw new InvalidOperationException();

    string IFSharpDeclaration.CompiledName => throw new InvalidOperationException();
    void IFSharpDeclaration.SetName(string name, ChangeNameKind changeNameKind) => throw new InvalidOperationException();
    TreeTextRange IFSharpDeclaration.GetNameIdentifierRange() => throw new InvalidOperationException();
    XmlDocBlock IFSharpDeclaration.XmlDocBlock => null;
    IFSharpIdentifier INameIdentifierOwner.NameIdentifier => throw new InvalidOperationException();

    XmlNode IXmlDocOwnerTreeNode.GetXMLDoc(bool inherit) => throw new InvalidOperationException();
  }

  internal class LocalBinding : LocalBindingStub
  {
    public override ITypeUsage SetTypeUsage(ITypeUsage typeUsage)
    {
      if (TypeUsage != null)
        return base.SetTypeUsage(typeUsage);

      var anchor = (ITreeNode)ParametersDeclarationsEnumerable.LastOrDefault() ?? HeadPattern;

      var factory = this.CreateElementFactory();
      var returnTypeInfo = ModificationUtil.AddChildAfter(anchor, factory.CreateReturnTypeInfo(typeUsage));
      return returnTypeInfo.ReturnType;
    }
  }
}
