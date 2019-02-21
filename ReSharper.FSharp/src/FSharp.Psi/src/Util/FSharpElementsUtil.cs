using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  /// <summary>
  /// Map FSharpSymbol elements (as seen by FSharp.Compiler.Service) to declared elements.
  /// </summary>
  public static class FSharpElementsUtil
  {
    [CanBeNull]
    internal static ITypeElement GetTypeElement([NotNull] FSharpEntity entity, [NotNull] IPsiModule psiModule)
    {
      if (((FSharpSymbol) entity).DeclarationLocation == null || entity.IsByRef || entity.IsProvidedAndErased)
        return null;

      if (!entity.IsFSharpAbbreviation)
      {
        var clrName = FSharpTypesUtil.GetClrName(entity);
        return clrName != null
          ? TypeFactory.CreateTypeByCLRName(clrName, psiModule).GetTypeElement()
          : null;
      }

      var symbolScope = psiModule.GetSymbolScope();
      while (entity.IsFSharpAbbreviation)
      {
        // FCS returns Clr names for non-abbreviated types only, using fullname
        var typeElement = TryFindByNames(GetPossibleNames(entity), symbolScope);
        if (typeElement != null)
          return typeElement;

        var abbreviatedType = entity.AbbreviatedType;
        if (!abbreviatedType.HasTypeDefinition)
          return null;

        entity = entity.AbbreviatedType.TypeDefinition;
      }

      var name = FSharpTypesUtil.GetClrName(entity);
      return name != null ? TypeFactory.CreateTypeByCLRName(name, psiModule).GetTypeElement() : null;
    }

    private static IEnumerable<string> GetPossibleNames([NotNull] FSharpEntity entity)
    {
      yield return entity.AccessPath + "." + entity.DisplayName;
      yield return entity.AccessPath + "." + entity.LogicalName;
      yield return ((FSharpSymbol) entity).FullName;
    }

    [CanBeNull]
    private static ITypeElement TryFindByNames([NotNull] IEnumerable<string> names, ISymbolScope symbolScope)
    {
      foreach (var name in names)
        if (symbolScope.GetElementsByQualifiedName(name).FirstOrDefault() is ITypeElement typeElement)
          return typeElement;
      return null;
    }

    [CanBeNull]
    private static INamespace GetDeclaredNamespace([NotNull] FSharpEntity entity, IPsiModule psiModule)
    {
      var name = entity.LogicalName;
      var containingNamespace = entity.Namespace?.Value;
      var fullName = containingNamespace != null ? containingNamespace + "." + name : name;
      var elements = psiModule.GetSymbolScope().GetElementsByQualifiedName(fullName);
      return elements.FirstOrDefault() as INamespace;
    }

    [CanBeNull]
    public static IDeclaredElement GetDeclaredElement([CanBeNull] FSharpSymbol symbol,
      [NotNull] IPsiModule psiModule, [CanBeNull] FSharpIdentifierToken referenceOwnerToken = null)
    {
      if (symbol == null)
        return null;

      if (symbol is FSharpEntity entity)
      {
        if (entity.IsUnresolved)
          return null;

        if (entity.IsNamespace)
          return GetDeclaredNamespace(entity, psiModule);

        return GetTypeElement(entity, psiModule);
      }

      if (symbol is FSharpMemberOrFunctionOrValue mfv)
      {
        if (mfv.IsUnresolved) return null;

        if (!mfv.IsModuleValueOrMember)
        {
          var declaration = FindNode<IFSharpDeclaration>(mfv.DeclarationLocation, referenceOwnerToken);
          if (declaration is IFSharpLocalDeclaration localDeclaration)
            return localDeclaration;

          return declaration is ISynPat
            ? declaration.DeclaredElement
            : null;
        }

        var memberEntity = mfv.IsModuleValueOrMember ? mfv.DeclaringEntity : null;
        if (memberEntity == null) return null;

        if (mfv.IsImplicitConstructor)
          return GetDeclaredElement(memberEntity.Value, psiModule, referenceOwnerToken);

        var typeElement = GetTypeElement(memberEntity.Value, psiModule);
        if (typeElement == null) return null;

        var members = mfv.IsConstructor
          ? typeElement.Constructors.AsList<ITypeMember>()
          : typeElement.EnumerateMembers(mfv.GetMemberCompiledName(), true).AsList();

        switch (members.Count)
        {
          case 0:
            return null;
          case 1:
            return members[0];
        }

        if (mfv.IsExtensionMember && mfv.IsInstanceMember)
        {
          var extensionMember = members.FirstOrDefault(m =>
            m is IFSharpExtensionTypeMember e && (e.ApparentEntity?.Equals(mfv.ApparentEnclosingEntity) ?? false));
          if (extensionMember != null)
            return extensionMember;
        }

        var mfvXmlDocId = GetXmlDocId(mfv);
        return members.FirstOrDefault(m => m.XMLDocId == mfvXmlDocId);
      }

      if (symbol is FSharpUnionCase unionCase)
      {
        if (unionCase.IsUnresolved) return null;

        var unionTypeElement = GetTypeElement(unionCase.ReturnType.TypeDefinition, psiModule);
        if (unionTypeElement == null) return null;

        var caseCompiledName = unionCase.CompiledName;
        var caseMember = unionTypeElement.GetMembers().FirstOrDefault(m =>
        {
          var shortName = m.ShortName;
          return shortName == caseCompiledName || shortName == "New" + caseCompiledName;
        });

        if (caseMember != null)
          return caseMember;

        var unionClrName = unionTypeElement.GetClrName();
        var caseDeclaredType = TypeFactory.CreateTypeByCLRName(unionClrName + "+" + caseCompiledName, psiModule);
        return caseDeclaredType.GetTypeElement();
      }

      if (symbol is FSharpField field && !field.IsUnresolved)
        return GetTypeElement(field.DeclaringEntity, psiModule)?.EnumerateMembers(field.Name, true).FirstOrDefault();

      // find active pattern entity/member. if it's compiled use wrapper. if it's source defined find actual element
      if (symbol is FSharpActivePatternCase activePatternCase)
        return GetActivePatternCaseElement(activePatternCase, psiModule, referenceOwnerToken);

      return null;
    }

    public static IDeclaredElement GetActivePatternCaseElement([NotNull] FSharpActivePatternCase activePatternCase,
      [NotNull] IPsiModule psiModule, [CanBeNull] FSharpIdentifierToken referenceOwnerToken)
    {
      var declaration = GetActivePatternDeclaration(activePatternCase, psiModule, referenceOwnerToken);
      return declaration?.GetActivePatternByIndex(activePatternCase.Index);
    }

    private static IFSharpDeclaration GetActivePatternDeclaration([NotNull] FSharpActivePatternCase activePatternCase,
      [NotNull] IPsiModule psiModule, FSharpIdentifierToken referenceOwnerToken)
    {
      var activePattern = activePatternCase.Group;
      var declaringEntity = activePattern.DeclaringEntity?.Value;
      if (declaringEntity != null)
      {
        var patternName = activePattern.PatternName();
        var typeElement = GetTypeElement(declaringEntity, psiModule);
        var patternElement = typeElement.EnumerateMembers(patternName, true).FirstOrDefault();
        return patternElement?.GetDeclarations().FirstOrDefault() as IFSharpDeclaration;
      }

      var patternId = FindNode<IActivePatternId>(activePatternCase.DeclarationLocation, referenceOwnerToken);
      return patternId?.GetContainingNode<IFSharpDeclaration>();
    }

    [CanBeNull]
    private static string GetXmlDocId([NotNull] FSharpMemberOrFunctionOrValue mfv)
    {
      try
      {
        return mfv.XmlDocSig;
      }
      catch (Exception e)
      {
        Logger.LogMessage(LoggingLevel.WARN, "Could not get XmlDocId for {0}", mfv);
        Logger.LogExceptionSilently(e);
        return null;
      }
    }

    private static T FindNode<T>(Range.range range, [CanBeNull] ITreeNode node) where T : class, ITreeNode
    {
      var fsFile = node?.GetContainingFile() as IFSharpFile;
      var document = fsFile?.GetSourceFile()?.Document;
      if (document == null) return null;

      var idToken = fsFile.FindTokenAt(document.GetTreeEndOffset(range) - 1);
      return idToken?.GetContainingNode<T>(true);
    }
  }
}
