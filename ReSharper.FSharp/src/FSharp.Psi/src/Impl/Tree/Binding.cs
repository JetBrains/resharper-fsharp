using System.Linq;
using System.Xml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class Binding : ITypeMemberDeclaration
  {
    /// A workaround for getting a declared element for binding in features like Find Usages results
    /// where we're looking for containing type member.
    private ITypeMemberDeclaration FirstDeclaration =>
      HeadPattern is ITypeMemberDeclaration headPattern
        ? headPattern
        : HeadPattern.Declarations.FirstOrDefault();

    public ITypeMember DeclaredElement => FirstDeclaration?.DeclaredElement;
    IDeclaredElement IDeclaration.DeclaredElement => DeclaredElement;

    public string DeclaredName => SharedImplUtil.MISSING_DECLARATION_NAME;
    public TreeTextRange GetNameRange() => HeadPattern.GetTreeTextRange();

    public ITypeDeclaration GetContainingTypeDeclaration() => GetContainingNode<ITypeDeclaration>();

    public XmlNode GetXMLDoc(bool inherit) => null;
    public bool IsSynthetic() => false;

    public void SetName(string name)
    {
    }
  }
}
