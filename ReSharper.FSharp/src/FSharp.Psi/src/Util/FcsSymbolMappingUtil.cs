using System;
using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.Compiled;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl.Resolve;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Range = FSharp.Compiler.Text.Range;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  /// Maps FSharpSymbol elements (produced by FSharp.Compiler.Service) to declared elements.
  public static class FcsSymbolMappingUtil
  {
    [CanBeNull]
    public static ITypeElement GetTypeElement([NotNull] this FSharpEntity entity, [NotNull] IPsiModule psiModule)
    {
      if (((FSharpSymbol) entity).DeclarationLocation == null || entity.IsByRef || entity.IsProvidedAndErased)
        return null;

      if (!entity.IsFSharpAbbreviation)
      {
        var clrTypeName = entity.GetClrName();
        if (clrTypeName == null)
          return null;

        var typeElements = psiModule.GetSymbolScope(false).GetTypeElementsByCLRName(clrTypeName);
        if (typeElements.IsEmpty())
        {
          if (entity.IsProvidedAndGenerated && // hide ProvidedAbbreviations in psi module?
              TypeProvidersContext.ProvidedAbbreviations.TryGetValue(clrTypeName.FullName, out var providedType) &&
              providedType.DeclaringType != null)
            return providedType.MapType(psiModule).GetTypeElement();
          return null; 
        }

        if (typeElements.Length == 1)
          return typeElements[0];

        // If there are multiple entities with given FQN, try to choose one based on assembly name or entity kind.
        var fcsAssemblySimpleName = entity.Assembly?.SimpleName;
        if (fcsAssemblySimpleName == null)
          return null;

        var isModule = entity.IsFSharpModule;
        return typeElements.FirstOrDefault(typeElement =>
        {
          if (GetModuleName(typeElement.Module) != fcsAssemblySimpleName)
            return false;

          // Happens when there are an exception and a module with the same name.
          // It's now allowed, but we want keep resolve working where possible.
          if (typeElement is IFSharpDeclaredElement)
            return isModule == typeElement is IFSharpModule;

          return true;
        });
      }

      var symbolScope = psiModule.GetSymbolScope(false);
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

      return entity.GetTypeElement(psiModule);
    }

    private static string GetModuleName([NotNull] IPsiModule psiModule) =>
      psiModule.ContainingProjectModule is IProject project
        ? project.GetOutputAssemblyName(psiModule.TargetFrameworkId)
        : psiModule.DisplayName;

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
      var elements = psiModule.GetSymbolScope(false).GetElementsByQualifiedName(fullName);
      return elements.FirstOrDefault() as INamespace;
    }

    [CanBeNull]
    public static IDeclaredElement GetDeclaredElement([CanBeNull] this FSharpSymbol symbol,
      [NotNull] IPsiModule psiModule, [CanBeNull] IFSharpReferenceOwner referenceExpression = null)
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

        return mfv.IsModuleValueOrMember
          ? GetTypeMember(mfv, psiModule)
          : GetLocalValueDeclaredElement(mfv, referenceExpression);
      }

      if (symbol is FSharpUnionCase unionCase)
        return GetDeclaredElement(unionCase, psiModule);

      if (symbol is FSharpField field)
      {
        if (field.IsAnonRecordField && referenceExpression != null)
          return new FSharpAnonRecordFieldProperty(referenceExpression.Reference);

        if (field.IsUnionCaseField && field.DeclaringUnionCase?.Value is { } fieldUnionCase)
        {
          var unionEntity = fieldUnionCase.ReturnType.TypeDefinition;
          var fieldOwnerTypeElement =
            unionEntity.UnionCases.Count > 1 && !unionEntity.IsValueType
              ? GetDeclaredElement(fieldUnionCase, psiModule, true) as ITypeElement
              : GetTypeElement(unionEntity, psiModule);

          return fieldOwnerTypeElement?.EnumerateMembers(field.Name, true).FirstOrDefault();
        }

        if (!field.IsUnresolved && field.DeclaringEntity?.Value is { } fieldEntity)
          return GetTypeElement(fieldEntity, psiModule)?.EnumerateMembers(field.Name, true).FirstOrDefault();
      }

      if (symbol is FSharpActivePatternCase patternCase)
        return GetActivePatternCaseElement(psiModule, referenceExpression, patternCase);

      if (symbol is FSharpGenericParameter genericParameter)
        return GetTypeParameter(genericParameter, referenceExpression);

      if (symbol is FSharpParameter parameter && referenceExpression != null)
        return parameter.GetOwner(referenceExpression.Reference); // todo: map to parameter

      return null;
    }

    [CanBeNull]
    public static IDeclaredElement GetDeclaredElement([CanBeNull] this FSharpSymbol symbol,
      [NotNull] FSharpSymbolReference reference) =>
      symbol != null && reference.GetElement() is var referenceOwner
        ? symbol.GetDeclaredElement(referenceOwner.GetPsiModule(), referenceOwner)
        : null;

    private static IDeclaredElement GetDeclaredElement(FSharpUnionCase unionCase, IPsiModule psiModule,
      bool preferType = false)
    {
      if (unionCase.IsUnresolved) return null;

      var unionTypeElement = GetTypeElement(unionCase.ReturnType.TypeDefinition, psiModule);
      if (unionTypeElement == null) return null;

      var caseCompiledName = unionCase.CompiledName;
      var caseMember = unionTypeElement.GetMembers().FirstOrDefault(m =>
      {
        if (preferType)
        {
          if (!(m is FSharpUnionCaseClass))
            return false;
        }
        else if (m is IFSharpGeneratedFromUnionCase)
          return false;

        var shortName = m.ShortName;
        return shortName == caseCompiledName || shortName == "New" + caseCompiledName;
      });

      if (caseMember != null)
        return caseMember;

      var unionClrName = unionTypeElement.GetClrName();
      var caseDeclaredType = TypeFactory.CreateTypeByCLRName(unionClrName + "+" + caseCompiledName, psiModule);
      return caseDeclaredType.GetTypeElement();
    }

    private static IDeclaredElement GetTypeMember([NotNull] FSharpMemberOrFunctionOrValue mfv,
      [NotNull] IPsiModule psiModule)
    {
      Assertion.Assert(mfv.IsModuleValueOrMember);
      var entity = mfv.DeclaringEntity.NotNull().Value;

      var typeElement = GetTypeElement(entity, psiModule);
      if (typeElement == null)
        return null;

      // todo: provided types: return provided member, use FSharpSearcherFactory.GetNavigateToTargets instead
      if (entity.IsProvided)
        return typeElement;

      return typeElement is IFSharpTypeElement fsTypeElement && !mfv.IsConstructor
        ? GetFSharpSourceTypeMember(mfv, fsTypeElement)
        : GetTypeMember(mfv, typeElement);
    }

    private static IDeclaredElement GetFSharpSourceTypeMember([NotNull] FSharpMemberOrFunctionOrValue mfv,
      [NotNull] IFSharpTypeElement fsTypeElement)
    {
      var name = mfv.GetMfvCompiledName();

      var symbolTableCache = fsTypeElement.GetPsiServices().Caches.GetPsiCache<SymbolTableCache>();
      var symbolTable = symbolTableCache.TryGetCachedSymbolTable(fsTypeElement, SymbolTableMode.FULL);
      if (symbolTable != null)
      {
        var members = symbolTable.GetSymbolInfos(name).SelectNotNull(info =>
          info.GetDeclaredElement() is ITypeMember member && member.ShortName == name ? member : null);
        return ChooseTypeMember(mfv, members.AsList());
      }

      var fcsRange = mfv.DeclarationLocation;
      var path = VirtualFileSystemPath.TryParse(fcsRange.FileName, InteractionContext.SolutionContext);
      if (path.IsEmpty)
        return GetTypeMember(mfv, fsTypeElement);

      var declarations = new List<FSharpProperTypeMemberDeclarationBase>();
      var typeElement = (TypeElement) fsTypeElement;
      foreach (var typePart in typeElement.EnumerateParts())
      {
        var typeDeclaration = typePart.GetDeclaration() as FSharpTypeElementDeclarationBase;
        var sourceFile = typeDeclaration?.GetSourceFile();
        if (sourceFile == null || sourceFile.GetLocation() != path)
          continue;

        foreach (var declaration in typeDeclaration.MemberDeclarations)
        {
          if (declaration is FSharpProperTypeMemberDeclarationBase fsDeclaration && fsDeclaration.CompiledName == name)
            declarations.Add(fsDeclaration);
        }
      }

      if (declarations.Count == 0)
        return GetTypeMember(mfv, typeElement);

      var singleDeclaration = declarations.SingleItem(decl =>
      {
        var range = decl.GetSourceFile().NotNull().Document.GetTreeTextRange(fcsRange);
        return range.Contains(decl.GetNameIdentifierRange());
      });

      var singleSourceElement = singleDeclaration?.GetOrCreateDeclaredElement(mfv);
      if (singleSourceElement != null)
        return singleSourceElement;

      if (mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod && !mfv.IsImplicitAccessor())
        return GetTypeMember(mfv, typeElement);

      return null;
    }

    private static IDeclaredElement GetTypeMember([NotNull] FSharpMemberOrFunctionOrValue mfv,
      [NotNull] ITypeElement typeElement)
    {
      var compiledName = mfv.GetMfvCompiledName();
      var members = mfv.IsConstructor
        ? typeElement.Constructors.AsList<ITypeMember>()
        : typeElement.EnumerateMembers(compiledName, true).AsList();

      return ChooseTypeMember(mfv, members);
    }

    [CanBeNull]
    private static IDeclaredElement ChooseTypeMember(FSharpMemberOrFunctionOrValue mfv,
      [NotNull] IReadOnlyList<ITypeMember> members)
    {
      switch (members.Count)
      {
        case 0:
          return null;
        case 1:
          return members[0];
      }

      var mfvXmlDocId = GetXmlDocId(mfv);
      if (mfvXmlDocId.IsEmpty())
        return mfv.IsConstructor
          ? members.FirstOrDefault(member => member is IConstructor { IsDefault: true })
          : null;

      return members.FirstOrDefault(member =>
        // todo: Fix signature for extension properties
        member is IFSharpMember fsMember && fsMember.Mfv?.GetXmlDocId() == mfvXmlDocId ||
        member.XMLDocId == mfvXmlDocId);
    }

    private static IDeclaredElement GetLocalValueDeclaredElement(FSharpMemberOrFunctionOrValue mfv,
      IFSharpReferenceOwner referenceExpression)
    {
      var declaration = FindNode<IFSharpDeclaration>(mfv.DeclarationLocation, referenceExpression);
      if (declaration is IFSharpLocalDeclaration localDeclaration)
        return localDeclaration;

      return declaration is IFSharpPattern
        ? declaration.DeclaredElement
        : null;
    }

    public static IDeclaredElement GetActivePatternCaseElement(IPsiModule psiModule,
      IFSharpReferenceOwner referenceOwner, FSharpActivePatternCase patternCase)
    {
      var pattern = patternCase.Group;
      var entity = pattern.DeclaringEntity?.Value;
      if (entity == null)
        return GetSourceActivePatternCaseElement(patternCase, psiModule, referenceOwner);

      var typeElement = GetTypeElement(entity, psiModule);
      var patternName = pattern.Name?.Value;
      if (typeElement == null || patternName == null)
        return null;

      if (typeElement.Module.ContainingProjectModule is IProject)
        return GetSourceActivePatternCaseElement(patternCase, psiModule, referenceOwner);

      var patternMfv = entity.MembersFunctionsAndValues.FirstOrDefault(mfv => mfv.LogicalName == patternName);
      var patternCompiledName = patternMfv?.CompiledName ?? patternCase.Name;
      if (typeElement.EnumerateMembers(patternCompiledName, true).FirstOrDefault() is IMethod method)
        return new CompiledActivePatternCase(method, patternCase.Name, patternCase.Index);

      return null;
    }

    [CanBeNull]
    private static ITypeParameter GetTypeParameter([NotNull] FSharpGenericParameter parameter,
      [CanBeNull] IFSharpReferenceOwner referenceOwnerToken = null)
    {
      var containingMemberDeclaration = referenceOwnerToken?.GetContainingNode<ITypeMemberDeclaration>();
      if (!(containingMemberDeclaration?.DeclaredElement is IFSharpTypeParametersOwner containingMember))
        return null;

      var parameterName = parameter.Name;
      var typeParameter = containingMember.AllTypeParameters.FirstOrDefault(param => param.ShortName == parameterName);
      return typeParameter;
    }

    public static IDeclaredElement GetSourceActivePatternCaseElement([NotNull] FSharpActivePatternCase fcsActivePatternCase,
      [NotNull] IPsiModule psiModule, [CanBeNull] IFSharpReferenceOwner referenceOwnerToken)
    {
      var declaration = GetActivePatternDeclaration(fcsActivePatternCase, psiModule, referenceOwnerToken);
      return declaration?.GetActivePatternCaseByIndex(fcsActivePatternCase.Index);
    }

    private static IFSharpDeclaration GetActivePatternDeclaration([NotNull] FSharpActivePatternCase fcsActivePatternCase,
      [NotNull] IPsiModule psiModule, IFSharpReferenceOwner referenceOwnerToken)
    {
      var activePattern = fcsActivePatternCase.Group;
      var declaringEntity = activePattern.DeclaringEntity?.Value;
      if (declaringEntity != null)
      {
        var patternName = activePattern.PatternName();
        var typeElement = GetTypeElement(declaringEntity, psiModule);
        var patternElement = typeElement.EnumerateMembers(patternName, true).FirstOrDefault();
        return patternElement?.GetDeclarations().FirstOrDefault() as IFSharpDeclaration;
      }

      var patternId = FindNode<IActivePatternId>(fcsActivePatternCase.DeclarationLocation, referenceOwnerToken);
      return patternId?.GetContainingNode<IFSharpDeclaration>();
    }

    [NotNull]
    public static string GetXmlDocId([NotNull] this FSharpMemberOrFunctionOrValue mfv)
    {
      try
      {
        var xmlDocId = mfv.XmlDocSig;
        if (xmlDocId.StartsWith("P", StringComparison.Ordinal) && mfv.IsCliEvent())
          return "E" + xmlDocId.Substring(1);

        return xmlDocId;
      }
      catch (Exception e)
      {
        Logger.LogMessage(LoggingLevel.WARN, "Could not get XmlDocId for {0}", mfv);
        Logger.LogExceptionSilently(e);
        return "";
      }
    }

    public static bool IsCliEvent(this FSharpMemberOrFunctionOrValue mfv) =>
      mfv.IsProperty && mfv.Attributes.HasAttributeInstance(FSharpPredefinedType.CLIEventAttribute);

    public static bool IsAccessor([NotNull] this FSharpMemberOrFunctionOrValue mfv) =>
      mfv.IsPropertyAccessor() || mfv.IsCliEventAccessor();

    public static bool IsCliEventAccessor([NotNull] this FSharpMemberOrFunctionOrValue mfv) =>
      mfv.IsEventAddMethod || mfv.IsEventRemoveMethod || mfv.IsPropertyGetterMethod &&
      (mfv.AccessorProperty?.Value.IsCliEvent() ?? false);

    public static bool IsPropertyAccessor([CanBeNull] this FSharpSymbol fcsSymbol) =>
      fcsSymbol is FSharpMemberOrFunctionOrValue mfv && (mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod);

    public static bool IsNonCliEventProperty([CanBeNull] this FSharpSymbol fcsSymbol) =>
      fcsSymbol is FSharpMemberOrFunctionOrValue { IsProperty: true } mfv && !mfv.IsCliEvent();

    public static bool IsNonCliEventPropertyOrAccessor([CanBeNull] this FSharpSymbol fcsSymbol) =>
      fcsSymbol.IsNonCliEventProperty() || fcsSymbol.IsPropertyAccessor();

    private static T FindNode<T>(Range range, [CanBeNull] ITreeNode node) where T : class, ITreeNode
    {
      var fsFile = node?.GetContainingFile() as IFSharpFile;
      var document = fsFile?.GetSourceFile()?.Document;
      if (document == null) return null;

      var idToken = fsFile.FindTokenAt(document.GetTreeEndOffset(range) - 1);
      return idToken?.GetContainingNode<T>(true);
    }

    public static IList<IParameter> GetParameters<T>(this T function, FSharpMemberOrFunctionOrValue mfv)
      where T : IParametersOwner, IFSharpTypeParametersOwner
    {
      if (mfv == null)
        return EmptyList<IParameter>.Instance;

      var paramGroups = mfv.CurriedParameterGroups;
      var isFsExtension = mfv.IsExtensionMember;
      var isVoidReturn = paramGroups.Count == 1 && paramGroups[0].Count == 1 && paramGroups[0][0].Type.IsUnit;

      if (!isFsExtension && isVoidReturn)
        return EmptyArray<IParameter>.Instance;

      var paramsCount = paramGroups.Sum(list => list.Count);
      if (paramsCount == 0)
        return EmptyList<IParameter>.Instance;

      var typeParameters = function.AllTypeParameters;
      var methodParams = new List<IParameter>(paramsCount);
      if (isFsExtension && mfv.IsInstanceMember)
      {
        var typeElement = mfv.ApparentEnclosingEntity.GetTypeElement(function.Module);

        var type =
          typeElement != null
            ? TypeFactory.CreateType(typeElement)
            : TypeFactory.CreateUnknownType(function.Module);

        methodParams.Add(new FSharpExtensionMemberParameter(function, type));
      }

      if (isVoidReturn)
        return methodParams;

      foreach (var paramsGroup in paramGroups)
      foreach (var param in paramsGroup)
        methodParams.Add(new FSharpMethodParameter(param, function, methodParams.Count,
          param.Type.MapType(typeParameters, function.Module, true)));

      return methodParams;
    }
  }
}
