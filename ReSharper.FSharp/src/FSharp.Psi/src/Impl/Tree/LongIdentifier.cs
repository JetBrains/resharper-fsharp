using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LongIdentifier
  {
    public TreeNodeCollection<ITokenNode> Qualifiers =>
      Identifiers.IsEmpty
        ? TreeNodeCollection<ITokenNode>.Empty
        : new TreeNodeCollection<ITokenNode>(Identifiers.Take(Identifiers.Count - 1).ToArray());

    public string QualifiedName =>
      Identifiers.IsEmpty
        ? SharedImplUtil.MISSING_DECLARATION_NAME
        : Identifiers.Select(id => id.GetText().RemoveBackticks()).Join(StringUtil.SDOT);

    public string Name =>
      Identifiers.IsEmpty
        ? SharedImplUtil.MISSING_DECLARATION_NAME
        : Identifiers.Last().GetText().RemoveBackticks();
  }
}