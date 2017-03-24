using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Searching
{
  internal class FSharpReferenceSearchProcessor<TResult> : ReferenceSearchSourceFileProcessor<TResult>
  {
    private readonly List<FSharpSymbol> myFSharpSymbols;
    private readonly IPsiModule myPsiModule;

    public FSharpReferenceSearchProcessor(ITreeNode treeNode, bool findCandidates,
      IFindResultConsumer<TResult> resultConsumer, IDeclaredElementsSet elements, ICollection<string> wordsInText,
      ICollection<string> referenceNames)
      : base(treeNode, findCandidates, resultConsumer, elements, wordsInText, referenceNames)
    {
      myFSharpSymbols = new List<FSharpSymbol>(elements.Select(FSharpElementsUtil.GetFSharpSymbolFromElement).WhereNotNull());
      myPsiModule = treeNode.GetPsiModule();
    }

    protected override bool AcceptElement(IDeclaredElement resolvedElement)
    {
      var symbol = FSharpElementsUtil.GetFSharpSymbolFromElement(resolvedElement);
      Assertion.AssertNotNull(symbol, "resolvedSymbol != null");

      if (myFSharpSymbols.Any(s => s.IsEffectivelySameAs(symbol))) return true;
      var actualElement = FSharpElementsUtil.GetDeclaredElement(symbol, myPsiModule);
      return actualElement != null && Elements.Contains(actualElement);
    }
  }
}