using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TopBinding : IFunctionDeclaration
  {
    /// A workaround for getting a declared element for binding in features like Find Usages results and
    /// file member navigation where we're looking for containing type member.
    [CanBeNull]
    private ITypeMemberDeclaration FirstDeclaration =>
      HeadPattern is ITypeMemberDeclaration headPattern
        ? headPattern
        : HeadPattern?.NestedPatterns.FirstOrDefault() as ITypeMemberDeclaration;

    public ITypeMember DeclaredElement => FirstDeclaration?.DeclaredElement;
    IDeclaredElement IDeclaration.DeclaredElement => DeclaredElement;

    public string DeclaredName => SharedImplUtil.MISSING_DECLARATION_NAME;

    public TreeNodeCollection<IAttribute> AllAttributes
    {
      get
      {
        var attributes = Attributes;

        var letBindings = LetBindingsDeclarationNavigator.GetByBinding(this);
        if (letBindings == null)
          return attributes;

        if (letBindings.BindingsEnumerable.FirstOrDefault() != this)
          return attributes;

        var letAttributes = letBindings.Attributes;
        if (letAttributes.IsEmpty)
          return attributes;

        return attributes.IsEmpty
          ? letAttributes
          : attributes.Prepend(letAttributes).ToTreeNodeCollection();
      }
    }

    public TreeTextRange GetNameRange()
    {
      var headPattern = HeadPattern;
      if (headPattern == null)
        return TreeTextRange.InvalidRange;

      return headPattern.Declarations.SingleItem()?.GetNameRange() ?? headPattern.GetTreeTextRange();
    }

    public ITypeDeclaration GetContainingTypeDeclaration() => GetContainingNode<ITypeDeclaration>();

    public XmlNode GetXMLDoc(bool inherit) => null;
    public bool IsSynthetic() => false;

    IFunction IFunctionDeclaration.DeclaredElement =>
      FirstDeclaration?.DeclaredElement as IFunction;

    public void SetName(string name)
    {
    }

    public bool IsMutable => MutableKeyword != null;

    public void SetIsMutable(bool value)
    {
      if (!value)
        throw new System.NotImplementedException();

      var headPat = HeadPattern;
      if (headPat != null)
        FSharpImplUtil.AddTokenBefore(headPat, FSharpTokenType.MUTABLE);
    }

    public bool HasParameters => !ParametersDeclarationsEnumerable.IsEmpty();
  }
}
