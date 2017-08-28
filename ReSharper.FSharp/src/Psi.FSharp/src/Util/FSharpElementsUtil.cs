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
        return TypeFactory.CreateTypeByCLRName(FSharpTypesUtil.GetClrName(entity), psiModule).GetTypeElement();

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

      return TypeFactory.CreateTypeByCLRName(FSharpTypesUtil.GetClrName(entity), psiModule).GetTypeElement();
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
      {
        var typeElement = symbolScope.GetElementsByQualifiedName(name).FirstOrDefault() as ITypeElement;
        if (typeElement != null)
          return typeElement;
      }
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

      var entity = symbol as FSharpEntity;
      if (entity != null)
      {
        if (entity.IsUnresolved) return null;
        return entity.IsNamespace
          ? GetDeclaredNamespace(entity, psiModule)
          : GetTypeElement(entity, psiModule);
      }

      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      if (mfv != null)
      {
        if (mfv.IsUnresolved) return null;

        if (!mfv.IsModuleValueOrMember)
          return FindLocalDeclaration(mfv, referenceOwnerToken);

        var memberEntity = mfv.IsModuleValueOrMember ? mfv.EnclosingEntity : null;
        if (memberEntity == null) return null;

        if (mfv.IsImplicitConstructor)
          return GetDeclaredElement(memberEntity.Value, psiModule, referenceOwnerToken);

        var typeElement = GetTypeElement(memberEntity.Value, psiModule);
        if (typeElement == null) return null;

        var members = mfv.IsConstructor
          ? typeElement.Constructors.AsList<ITypeMember>()
          : typeElement.EnumerateMembers(mfv.GetMemberCompiledName(), true).AsList();

        var mfvXmlDocId = GetXmlDocId(mfv);
        return members.Count == 1
          ? members[0]
          : members.FirstOrDefault(m => mfvXmlDocId.Equals(m.XMLDocId, StringComparison.Ordinal));
      }

      var unionCase = symbol as FSharpUnionCase;
      if (unionCase != null)
      {
        if (unionCase.IsUnresolved) return null;

        var unionType = unionCase.ReturnType;
        Assertion.AssertNotNull(unionType, "unionType != null");
        var unionTypeElement = GetTypeElement(unionType.TypeDefinition, psiModule);
        if (unionTypeElement == null) return null;

        var caseMember = unionTypeElement.EnumerateMembers(unionCase.CompiledName, true).FirstOrDefault();
        if (caseMember != null)
          return caseMember;

        var newCaseMember = unionTypeElement.EnumerateMembers("New" + unionCase.CompiledName, true).FirstOrDefault();
        if (newCaseMember != null)
          return newCaseMember;

        var unionClrName = unionTypeElement.GetClrName();
        var caseDeclaredType = TypeFactory.CreateTypeByCLRName(unionClrName + "+" + unionCase.CompiledName, psiModule);
        return caseDeclaredType.GetTypeElement();
      }

      var field = symbol as FSharpField;
      if (field != null && !field.IsUnresolved)
      {
        var typeElement = GetTypeElement(field.DeclaringEntity, psiModule);
        return typeElement?.EnumerateMembers(field.Name, true).FirstOrDefault();
      }

      var activePatternCase = symbol as FSharpActivePatternCase;
      if (activePatternCase != null)
        return new ResolvedFSharpSymbolElement(activePatternCase, referenceOwnerToken);

      return null;
    }

    private static string GetXmlDocId([NotNull] FSharpMemberOrFunctionOrValue mfv)
    {
      lock (ourFcSLock)
        return mfv.XmlDocSig;
    }

    private static IClrDeclaredElement FindLocalDeclaration([NotNull] FSharpMemberOrFunctionOrValue mfv,
      [CanBeNull] FSharpIdentifierToken referenceOwnerToken)
    {
      var fsFile = referenceOwnerToken?.GetContainingFile() as IFSharpFile;
      var document = fsFile?.GetSourceFile()?.Document;
      if (document == null) return null;

      var idToken = fsFile.FindTokenAt(document.GetTreeEndOffset(mfv.DeclarationLocation) - 1);
      return idToken?.GetContainingNode<LocalDeclaration>();
    }
  }
}