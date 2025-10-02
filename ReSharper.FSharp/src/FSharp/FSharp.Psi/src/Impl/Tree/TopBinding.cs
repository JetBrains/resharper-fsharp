using System;
using System.Collections.Generic;
using System.Linq;
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
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TopBindingStub : IFunctionDeclaration
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
      ModificationUtil.DeleteChild(InlineKeyword);
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
    public bool IsComputed => false;

    [CanBeNull] private IReferencePat HeadReferencePat => HeadPattern as IReferencePat;

    IDeclaredElement IParameterOwnerMemberDeclaration.DeclaredElement =>
      HeadReferencePat?.DeclaredElement;

    string IFSharpDeclaration.SourceName =>
      HeadReferencePat?.SourceName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    IFSharpParameterDeclaration IFSharpParameterOwnerDeclaration.GetParameterDeclaration(FSharpParameterIndex index) =>
      this.GetBindingParameterPatterns().GetParameterDeclaration(index);

    IList<IList<IFSharpParameterDeclaration>> IFSharpParameterOwnerDeclaration.GetParameterDeclarations() =>
      this.GetBindingParameterDeclarations();

    IFSharpIdentifier INameIdentifierOwner.NameIdentifier => null;

    FSharpSymbol IFSharpDeclaration.GetFcsSymbol() => HeadReferencePat?.GetFcsSymbol();
    string IFSharpDeclaration.CompiledName => throw new InvalidOperationException();
    void IFSharpDeclaration.SetName(string name, ChangeNameKind changeNameKind) => throw new InvalidOperationException();
    TreeTextRange IFSharpDeclaration.GetNameIdentifierRange() => throw new InvalidOperationException();
    XmlDocBlock IFSharpDeclaration.XmlDocBlock => FirstChild as XmlDocBlock;
  }

  internal class TopBinding : TopBindingStub
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
