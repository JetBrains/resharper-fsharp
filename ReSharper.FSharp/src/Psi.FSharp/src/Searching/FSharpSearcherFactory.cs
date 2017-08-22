using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Impl.Search;
using JetBrains.ReSharper.Psi.Impl.Search.SearchDomain;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Searching
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
      if (localDeclaration != null)
        return mySearchDomainFactory.CreateSearchDomain(localDeclaration.GetSourceFile());

      var fsSymbolElement = declaredElement as IFSharpSymbolElement;
      if (fsSymbolElement != null)
      {
        var fsSymbol = fsSymbolElement.Symbol;
        var activePatternCase = fsSymbol as FSharpActivePatternCase;
        if (activePatternCase == null)
          return EmptySearchDomain.Instance;

        var resolvedSymbolElement = fsSymbolElement as ResolvedFSharpSymbolElement;
        if (resolvedSymbolElement != null)
        {
          var patternEntity = activePatternCase.Group.EnclosingEntity?.Value;
          if (patternEntity != null)
          {
            var patternTypeElement = FSharpElementsUtil.GetDeclaredElement(patternEntity, fsSymbolElement.Module);
            if (patternTypeElement == null)
              return EmptySearchDomain.Instance;

            declaredElement = patternTypeElement;
          }
        }

        var activePatternCaseElement = fsSymbolElement as ActivePatternCase;
        var declaration = activePatternCaseElement?.GetDeclaration();
        var containingType = ((ITypeDeclaration) declaration?.GetContainingTypeDeclaration())?.DeclaredElement;
        if (containingType != null)
          declaredElement = containingType;
      }

      return myClrSearchFactory.GetDeclaredElementSearchDomain(declaredElement);
    }

    public Tuple<ICollection<IDeclaredElement>, bool> GetNavigateToTargets(IDeclaredElement element)
    {
      // todo: for union cases navigate to case declarations

      var resolvedSymbolElement = element as ResolvedFSharpSymbolElement;
      var activePatternCase = resolvedSymbolElement?.Symbol as FSharpActivePatternCase;
      if (activePatternCase != null)
      {
        var activePattern = activePatternCase.Group;

        var entityOption = activePattern.EnclosingEntity;
        var patternNameOption = activePattern.Name;
        if (entityOption == null || patternNameOption == null) return null;

        var typeElement = FSharpElementsUtil.GetTypeElement(entityOption.Value, resolvedSymbolElement.Module);
        var pattern = typeElement.EnumerateMembers(patternNameOption.Value, true).FirstOrDefault() as IDeclaredElement;
        var fsDeclaredTypeMember = pattern as IFSharpTypeMember;
        if (fsDeclaredTypeMember != null)
        {
          var patternDecl = pattern.GetDeclarations().FirstOrDefault();
          if (patternDecl == null)
            return null;

          var caseElement = FSharpImplUtil.GetActivePatternByIndex(patternDecl, activePatternCase.CaseIndex);
          if (caseElement != null)
            return Tuple.Create(new[] {caseElement}.AsCollection(), false);
        }
        else if (pattern != null)
        {
          return Tuple.Create(new[] {pattern}.AsCollection(), false);
        }
      }

      var fsTypeMember = element as IFSharpTypeMember;
      if (fsTypeMember == null || fsTypeMember.IsVisibleFromFSharp)
        return null;

      var containingType = fsTypeMember.GetContainingType() as IDeclaredElement;
      return containingType != null
        ? Tuple.Create(new[] {containingType}.AsCollection(), false)
        : null;
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