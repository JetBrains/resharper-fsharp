using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Searching;
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
    private static readonly object ourFcSLock = new object();

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

      var symbolScope = psiModule.GetPsiServices().Symbols.GetSymbolScope(psiModule, true, true);
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
    private static IClrDeclaredElement GetDeclaredNamespace([NotNull] FSharpEntity entity, IPsiModule psiModule)
    {
      Assertion.Assert(entity.IsNamespace, "entity.IsNamespace");
      var name = entity.CompiledName;
      var containingName = entity.Namespace?.Value;
      var fullName = containingName != null ? containingName + "." + name : name;
      var symbolScope = psiModule.GetPsiServices().Symbols.GetSymbolScope(psiModule, true, true);
      return symbolScope.GetElementsByQualifiedName(fullName).FirstOrDefault() as INamespace;
    }

    [CanBeNull]
    public static IDeclaredElement GetDeclaredElement([CanBeNull] FSharpSymbol symbol,
      [NotNull] IPsiModule psiModule, [CanBeNull] FSharpIdentifierToken referenceOwnerToken = null)
    {
      if (symbol == null) return null;

      if (symbol is FSharpEntity entity)
      {
        if (entity.IsUnresolved) return null;
        return entity.IsNamespace
          ? GetDeclaredNamespace(entity, psiModule)
          : GetTypeElement(entity, psiModule);
      }

      if (symbol is FSharpMemberOrFunctionOrValue mfv)
      {
        if (mfv.IsUnresolved) return null;

        if (!mfv.IsModuleValueOrMember)
          return FindDeclaration<LocalDeclaration>(mfv.DeclarationLocation, referenceOwnerToken);

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

      if (symbol is FSharpActivePatternCase activePatternCase)
        return
          activePatternCase.Group.DeclaringEntity != null
          ? new ResolvedFSharpSymbolElement(activePatternCase, referenceOwnerToken)
          : FindDeclaration<ActivePatternCaseDeclaration>(activePatternCase.DeclarationLocation, referenceOwnerToken)?.DeclaredElement;

      return null;
    }

    [CanBeNull]
    private static string GetXmlDocId([NotNull] FSharpMemberOrFunctionOrValue mfv)
    {
      lock (ourFcSLock)
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
    }

    private static T FindDeclaration<T>(Range.range range, [CanBeNull] ITreeNode token) where T : class, IDeclaration
    {
      var fsFile = token?.GetContainingFile() as IFSharpFile;
      var document = fsFile?.GetSourceFile()?.Document;
      if (document == null) return null;

      var idToken = fsFile.FindTokenAt(document.GetTreeEndOffset(range) - 1);
      return idToken?.GetContainingNode<T>(true);
    }
  }
}