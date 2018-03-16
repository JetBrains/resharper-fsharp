using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Searching
{
  internal class FSharpReferenceSearchProcessor<TResult> : ReferenceSearchSourceFileProcessor<TResult>
  {
    private readonly IList<FSharpSymbol> myFSharpSymbols;

    public FSharpReferenceSearchProcessor(ITreeNode treeNode, bool findCandidates,
      IFindResultConsumer<TResult> resultConsumer, IDeclaredElementsSet elements, IList<FSharpSymbol> fSharpSymbols,
      ICollection<string> referenceNames) : base(treeNode, findCandidates, resultConsumer, elements, referenceNames,
      referenceNames)
    {
      myFSharpSymbols = fSharpSymbols;
    }

    protected override bool AcceptElement(IDeclaredElement resolvedElement)
    {
      // found a proper R# declared element
      if (Elements.Contains(resolvedElement))
        return true;

      if (resolvedElement is IConstructor constructor)
      {
        var containingType = constructor.GetContainingType();
        if (containingType != null)
          return AcceptElement(containingType);
      }
      
      // found a symbol that cannot be resolved to the R# cache,
      // e.g. an active pattern case declared in a compiled assembly
      var resolvedSymbol = (resolvedElement as IFSharpSymbolElement)?.Symbol;
      return resolvedSymbol != null && myFSharpSymbols.Any(s => s.IsEffectivelySameAs(resolvedSymbol));
    }
  }
}