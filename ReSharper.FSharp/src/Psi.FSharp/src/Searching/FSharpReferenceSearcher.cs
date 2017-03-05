using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Searching
{
  public class FSharpReferenceSearcher : IDomainSpecificSearcher
  {
    private readonly IDeclaredElementsSet myElements;
    private readonly bool myFindCandidates;
    private readonly ICollection<string> myElementNames;

    public FSharpReferenceSearcher(IDeclaredElementsSet elements, bool findCandidates)
    {
      myElements = elements;
      myFindCandidates = findCandidates;
      myElementNames = new List<string>();

      foreach (var element in elements)
        myElementNames.AddRange(FSharpNamesUtil.GetPossibleSourceNames(element));
    }

    public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
    {
      return sourceFile.GetPsiFiles<FSharpLanguage>().Any(file => ProcessElement(file, consumer));
    }

    public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
    {
      var result = new FSharpReferenceSearchProcessor<TResult>(element, myFindCandidates, consumer, myElements,
        myElementNames, myElementNames).Run();
      return result == FindExecution.Stop;
    }
  }
}