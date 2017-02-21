using System.Linq;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class LongIdentifier
  {
    public TreeNodeCollection<ITokenNode> Qualifiers =>
      new TreeNodeCollection<ITokenNode>(Identifiers.Take(Identifiers.Count - 1).ToArray());

    public string QualifiedName => Identifiers.Select(id => id.GetText()).Join(StringUtil.SDOT);

    public string ShortName => Identifiers.IsEmpty
      ? SharedImplUtil.MISSING_DECLARATION_NAME
      : Identifiers.Last().GetText();
  }
}