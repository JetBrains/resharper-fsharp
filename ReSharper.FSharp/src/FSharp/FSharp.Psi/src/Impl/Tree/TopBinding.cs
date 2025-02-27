using System;
using System.Linq;
using System.Xml;
using FSharp.Compiler.CodeAnalysis;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TopBinding : IFunctionDeclaration
  {
    /// A workaround for getting a declared element for binding in features like Find Usages results and
    /// file member navigation where we're looking for containing type member.
    [CanBeNull]
    private ITypeMemberDeclaration FirstDeclaration =>
      HeadPattern as ITypeMemberDeclaration ??
      HeadPattern?.NestedPatterns.FirstOrDefault() as ITypeMemberDeclaration;

    public ITypeMember DeclaredElement => FirstDeclaration?.DeclaredElement;
    public FSharpSymbolUse GetFcsSymbolUse() => (FirstDeclaration as IFSharpDeclaration)?.GetFcsSymbolUse();

    IDeclaredElement IDeclaration.DeclaredElement => DeclaredElement;

    public string DeclaredName => SharedImplUtil.MISSING_DECLARATION_NAME;

    public TreeTextRange GetNameRange()
    {
      var headPattern = HeadPattern;
      if (headPattern == null)
        return TreeTextRange.InvalidRange;

      return headPattern.Declarations.SingleItem()?.GetNameRange() ?? headPattern.GetTreeTextRange();
    }

    public ITypeDeclaration GetContainingTypeDeclaration() => GetContainingNode<ITypeDeclaration>();

    public XmlNode GetXMLDoc(bool inherit)
    {
      // The binding should not be used as an XMLDoc source by the logic of the F# PSI,
      // but when the nodes get bunch-processed as abstract instances of IXmlDocOwnerTreeNode,
      // it is quite unexpected to get an exception thrown at you as a PSI consumer.
      // Hence, the assertion is commented out.
      // Assertion.Fail("Unexpected call TopBinding.GetXMLDoc");
      return null;
    }

    public bool IsSynthetic() => false;

    IFunction IFunctionDeclaration.DeclaredElement =>
      FirstDeclaration?.DeclaredElement as IFunction;

    public void SetName(string name)
    {
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

    public bool IsMutable => MutableKeyword != null;

    public void SetIsMutable(bool value)
    {
      if (!value)
        throw new NotImplementedException();

      HeadPattern?.AddTokenBefore(FSharpTokenType.MUTABLE);
    }

    public bool HasParameters => !ParametersDeclarationsEnumerable.IsEmpty();
    public bool IsLiteral => Attributes.HasAttribute("Literal"); // todo: cache

    IDeclaredElement IParameterOwnerMemberDeclaration.DeclaredElement =>
      HeadPattern is IReferencePat rp
        ? rp.DeclaredElement
        : null;
  }
}
