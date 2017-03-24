using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.FSharp.Impl;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Impl.Search;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Searching
{
  [PsiSharedComponent]
  public class FSharpSearcherFactory : IDomainSpecificSearcherFactory
  {
    private readonly SearchDomainFactory mySearchDomainFactory;
    private readonly CLRDeclaredElementSearcherFactory myClrSearchFactory;

    public FSharpSearcherFactory(SearchDomainFactory searchDomainFactory,
      CLRDeclaredElementSearcherFactory clrSearchFactory)
    {
      mySearchDomainFactory = searchDomainFactory;
      myClrSearchFactory = clrSearchFactory;
    }

    public bool IsCompatibleWithLanguage(PsiLanguageType languageType)
    {
      return languageType.Is<FSharpLanguage>();
    }

    public IDomainSpecificSearcher CreateReferenceSearcher(IDeclaredElementsSet elements, bool findCandidates)
    {
      return new FSharpReferenceSearcher(elements, findCandidates);
    }

    public IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
    {
      return FSharpNamesUtil.GetPossibleSourceNames(element);
    }

    public Tuple<ICollection<IDeclaredElement>, bool> GetNavigateToTargets(IDeclaredElement element)
    {
      var fakeElement = element as FSharpFakeElementFromReference;
      var actualElement = fakeElement?.GetActualElement() as IDeclaredElement;
      return actualElement != null ? Tuple.Create(new[] {actualElement}.AsCollection(), false) : null;
    }

    public ICollection<FindResult> TransformNavigationTargets(ICollection<FindResult> targets)
    {
      return null;
    }

    public IEnumerable<RelatedDeclaredElement> GetRelatedDeclaredElements(IDeclaredElement element)
    {
      var actualElement = (element as FSharpFakeElementFromReference)?.GetActualElement();
      return actualElement != null
        ? (IEnumerable<RelatedDeclaredElement>) new[] {new RelatedDeclaredElement(actualElement)}
        : EmptyList<RelatedDeclaredElement>.Instance;
    }

    public ISearchDomain GetDeclaredElementSearchDomain(IDeclaredElement declaredElement)
    {
      var fakeElement = declaredElement as FSharpFakeElementFromReference;
      var actualElement = fakeElement?.GetActualElement();
      if (actualElement != null)
        myClrSearchFactory.GetDeclaredElementSearchDomain(actualElement);

      var mfv = fakeElement?.Symbol as FSharpMemberOrFunctionOrValue;
      if (mfv != null && !mfv.IsMember && !mfv.IsModuleValueOrMember)
        return mySearchDomainFactory.CreateSearchDomain(fakeElement.GetSourceFile());

      return mySearchDomainFactory.CreateSearchDomain(declaredElement.GetSolution(), false);
    }

    public Tuple<ICollection<IDeclaredElement>, Predicate<IFindResultReference>, bool> GetDerivedFindRequest(
      IFindResultReference result)
    {
      return null;
    }

    public IDomainSpecificSearcher CreateLateBoundReferenceSearcher(IDeclaredElementsSet elements)
    {
      return null;
    }

    public IDomainSpecificSearcher CreateConstructorSpecialReferenceSearcher(ICollection<IConstructor> constructors)
    {
      return null;
    }

    public IDomainSpecificSearcher CreateMethodsReferencedByDelegateSearcher(IDelegate @delegate)
    {
      return null;
    }

    public IDomainSpecificSearcher CreateTextOccurrenceSearcher(IDeclaredElementsSet elements)
    {
      return null;
    }

    public IDomainSpecificSearcher CreateTextOccurrenceSearcher(string subject)
    {
      return null;
    }

    public IDomainSpecificSearcher CreateAnonymousTypeSearcher(IList<AnonymousTypeDescriptor> typeDescription,
      bool caseSensitive)
    {
      return null;
    }

    public IDomainSpecificSearcher CreateConstantExpressionSearcher(ConstantValue constantValue,
      bool onlyLiteralExpression)
    {
      return null;
    }
  }
}