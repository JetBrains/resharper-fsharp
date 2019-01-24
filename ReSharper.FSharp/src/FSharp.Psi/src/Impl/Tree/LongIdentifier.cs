using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LongIdentifier
  {
    public TreeNodeCollection<ITokenNode> Qualifiers
    {
      get
      {
        var identifiers = Identifiers;
        return identifiers.IsEmpty
          ? TreeNodeCollection<ITokenNode>.Empty
          : new TreeNodeCollection<ITokenNode>(identifiers.Take(identifiers.Count - 1).ToArray());
      }
    }

    public string QualifiedName
    {
      get
      {
        var identifiers = Identifiers;
        return identifiers.IsEmpty
          ? SharedImplUtil.MISSING_DECLARATION_NAME
          : identifiers.Select(id => id.GetText().RemoveBackticks()).Join(StringUtil.SDOT);
      }
    }

    public string Name =>
      Identifiers.LastOrDefault()?.GetText().RemoveBackticks() ?? 
      SharedImplUtil.MISSING_DECLARATION_NAME;

    public ITokenNode IdentifierToken =>
      Identifiers.LastOrDefault();
  }
}