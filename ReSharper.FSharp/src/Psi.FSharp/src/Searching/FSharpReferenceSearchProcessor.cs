using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Searching
{
  internal class FSharpReferenceSearchProcessor<TResult> : ReferenceSearchSourceFileProcessor<TResult>
  {
    public FSharpReferenceSearchProcessor(ITreeNode treeNode, bool findCandidates,
      IFindResultConsumer<TResult> resultConsumer, IDeclaredElementsSet elements, ICollection<string> wordsInText,
      ICollection<string> referenceNames)
      : base(treeNode, findCandidates, resultConsumer, elements, wordsInText, referenceNames)
    {
    }

    protected override bool AcceptElement(IDeclaredElement resolvedElement)
    {
      return Elements.Contains(resolvedElement);
    }
  }
}