using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Impl.Search;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Util;

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

    public ISearchDomain GetDeclaredElementSearchDomain(IDeclaredElement declaredElement)
    {
      // todo: type abbreviations
      var localDeclaration = declaredElement as ILocalDeclaration;
      return localDeclaration != null
        ? mySearchDomainFactory.CreateSearchDomain(localDeclaration.GetSourceFile())
        : myClrSearchFactory.GetDeclaredElementSearchDomain(declaredElement);
    }

    public Tuple<ICollection<IDeclaredElement>, bool> GetNavigateToTargets(IDeclaredElement element)
    {
      return null;
    }

    public ICollection<FindResult> TransformNavigationTargets(ICollection<FindResult> targets)
    {
      return null;
    }

    public IEnumerable<RelatedDeclaredElement> GetRelatedDeclaredElements(IDeclaredElement element)
    {
      return EmptyList<RelatedDeclaredElement>.Instance;
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