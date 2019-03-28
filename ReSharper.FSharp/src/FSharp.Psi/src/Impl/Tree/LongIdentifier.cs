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
        if (identifiers.IsEmpty)
          return identifiers;

        var qualifiersCount = identifiers.Count - 1;
        var qualifiers = new ITokenNode[qualifiersCount];
        for (var i = 0; i < qualifiersCount; i++)
          qualifiers[i] = identifiers[i];
        return new TreeNodeCollection<ITokenNode>(qualifiers);
      }
    }

    public string QualifiedName =>
      Identifiers is var identifiers && !identifiers.IsEmpty
        ? identifiers.Select(id => id.GetText().RemoveBackticks()).Join(StringUtil.SDOT)
        : SharedImplUtil.MISSING_DECLARATION_NAME;

    public string Name =>
      Identifiers.LastOrDefault()?.GetText().RemoveBackticks() ??
      SharedImplUtil.MISSING_DECLARATION_NAME;

    public ITokenNode IdentifierToken =>
      Identifiers.LastOrDefault();
  }
}
