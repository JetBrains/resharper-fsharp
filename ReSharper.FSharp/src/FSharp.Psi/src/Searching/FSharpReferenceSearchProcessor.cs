using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Searching
{
  internal class FSharpReferenceSearchProcessor<TResult> : ReferenceSearchSourceFileProcessor<TResult>
  {
    public FSharpReferenceSearchProcessor(ITreeNode treeNode, bool findCandidates,
      IFindResultConsumer<TResult> resultConsumer, IDeclaredElementsSet elements, ICollection<string> referenceNames)
      : base(treeNode, findCandidates, resultConsumer, elements, referenceNames, referenceNames)
    {
    }
  }
}
