using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TopBinding : ITypeMemberDeclaration
  {
    /// A workaround for getting a declared element for binding in features like Find Usages results and
    /// file member navigation where we're looking for containing type member.
    [CanBeNull]
    private ITypeMemberDeclaration FirstDeclaration =>
      HeadPattern is ITypeMemberDeclaration headPattern
        ? headPattern
        : HeadPattern?.Declarations.FirstOrDefault();

    public ITypeMember DeclaredElement => FirstDeclaration?.DeclaredElement;
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

    public XmlNode GetXMLDoc(bool inherit) => null;
    public bool IsSynthetic() => false;

    public void SetName(string name)
    {
    }
  }
}
