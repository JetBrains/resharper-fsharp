using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Naming;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.Extension;
using JetBrains.Util.Logging;
using Microsoft.FSharp.Core;
using PrettyNaming = FSharp.Compiler.Syntax.PrettyNaming;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public static class FSharpImplUtil
  {
    public const string CompiledNameAttrName = "Microsoft.FSharp.Core.CompiledNameAttribute";
    public const string ModuleSuffixFlag = "CompilationRepresentationFlags.ModuleSuffix";
    public const string CompiledName = "CompiledName";
    public const string AttributeSuffix = "Attribute";
    public const string ModuleSuffix = "Module";
    public const string Interface = "Interface";
    public const string AbstractClass = "AbstractClass";
    public const string Class = "Class";
    public const string Sealed = "Sealed";
    public const string Struct = "Struct";
    public const string AutoOpen = "AutoOpen";

    [NotNull] public static string GetShortName([NotNull] this IAttribute attr) =>
      attr.ReferenceName?.ShortName.GetAttributeShortName() ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public static bool ShortNameEquals([NotNull] this IAttribute attr, [NotNull] string shortName) =>
      attr.GetShortName() == shortName;

    [CanBeNull]
    private static FSharpString GetStringConst([CanBeNull] IFSharpExpression expr)
    {
      switch (expr)
      {
        case IParenExpr parenExpr:
          return GetStringConst(parenExpr.InnerExpression);
        case IConstExpr constExpr:
          return constExpr.FirstChild as FSharpString;
      }
      return null;
    }

    private static bool GetCompiledNameValue(IAttribute attr, out string compiledName)
    {
      if (!attr.ShortNameEquals(CompiledName))
      {
        compiledName = null;
        return false;
      }

      // todo: proper expressions evaluation, e.g. "S1" + "S2"
      var stringArg = GetStringConst(attr.Expression);
      if (stringArg == null)
      {
        compiledName = null;
        return false;
      }

      compiledName =
        stringArg.GetText()
          .Substring(1, stringArg.GetText().Length - 2)
          .SubstringBeforeLast("`", StringComparison.Ordinal);
      return true;
    }

    private static bool IsModuleSuffixExpr([CanBeNull] IFSharpExpression expr)
    {
      switch (expr)
      {
        case ParenExpr parenExpr:
          return IsModuleSuffixExpr(parenExpr.InnerExpression);
        case IReferenceExpr referenceExpr:
          return referenceExpr.QualifiedName == ModuleSuffixFlag;
      }

      return false;
    }

    private static bool IsModuleSuffixAttribute([NotNull] this IAttribute attr) =>
      attr.ShortNameEquals("CompilationRepresentation") && IsModuleSuffixExpr(attr.Expression);

    public static string GetModuleCompiledName([CanBeNull] this IIdentifier identifier,
      TreeNodeCollection<IAttribute> attributes)
    {
      var hasModuleSuffix = false;

      foreach (var attr in attributes)
        if (attr.IsModuleSuffixAttribute())
        {
          hasModuleSuffix = true;
          break;
        }

      var sourceName = identifier?.Name;
      return hasModuleSuffix && sourceName != null
        ? sourceName + "Module"
        : sourceName ?? SharedImplUtil.MISSING_DECLARATION_NAME;
    }

    public static bool GetCompiledName(this TreeNodeCollection<IAttribute> attributes, out string name)
    {
      foreach (var attr in attributes)
        if (GetCompiledNameValue(attr, out var value))
        {
          name = value;
          return true;
        }

      name = default;
      return false;
    }

    [NotNull]
    public static string GetCompiledName([CanBeNull] this IIdentifier identifier,
      TreeNodeCollection<IAttribute> attributes) =>
      GetCompiledName(attributes, out var name)
        ? name
        : GetCompiledName(identifier);

    public static string GetCompiledName([CanBeNull] this IIdentifier identifier)
    {
      if (identifier == null)
        return SharedImplUtil.MISSING_DECLARATION_NAME;

      var name = identifier.Name;
      if (name == SharedImplUtil.MISSING_DECLARATION_NAME)
        return name;

      if (identifier is IFSharpIdentifierToken token &&
          token.GetTokenType() is var tokenType && tokenType != FSharpTokenType.IDENTIFIER)
        return tokenType == FSharpTokenType.LPAREN_STAR_RPAREN
          ? "op_Multiply"
          : PrettyNaming.CompileOpName(name);

      return name;
    }

    [NotNull]
    public static string GetSourceName([CanBeNull] this IIdentifier identifier) =>
      identifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public static TreeTextRange GetNameRange([CanBeNull] this IFSharpIdentifier identifier) =>
      // todo: fix navigating inside escaped names
      identifier?.NameRange ?? TreeTextRange.InvalidRange;

    public static TreeTextRange GetNameIdentifierRange([CanBeNull] this IFSharpIdentifier identifier) =>
      identifier?.GetNameRange() ?? TreeTextRange.InvalidRange;

    [NotNull]
    public static string MakeClrName([NotNull] this IFSharpTypeElementDeclaration declaration)
    {
      var clrName = new StringBuilder();

      var containingTypeDeclaration = declaration.GetContainingTypeDeclaration();
      if (containingTypeDeclaration != null)
      {
        clrName.Append(containingTypeDeclaration.CLRName).Append('+');
      }
      else
      {
        var namespaceDeclaration = declaration.GetContainingNamespaceDeclaration();
        if (namespaceDeclaration != null)
          clrName.Append(namespaceDeclaration.QualifiedName).Append('.');
      }

      clrName.Append(declaration.CompiledName);

      var typeDeclaration = declaration as IFSharpTypeDeclaration;
      var typeParametersCount = typeDeclaration?.TypeParameterDeclarations.Count ?? 0;
      if (typeParametersCount > 0)
        clrName.Append("`" + typeParametersCount);

      return clrName.ToString();
    }

    [NotNull]
    public static string GetMfvCompiledName([NotNull] this FSharpMemberOrFunctionOrValue mfv, ITypeElement typeElement)
    {
      try
      {
        var compiledNameAttr = mfv.Attributes.TryFindAttribute(CompiledNameAttrName);
        var compiledName = compiledNameAttr != null && !compiledNameAttr.Value.ConstructorArguments.IsEmpty()
          ? compiledNameAttr.Value.ConstructorArguments[0].Item2 as string
          : null;

        if (compiledName != null)
          return compiledName;

        if (IsImplicitAccessor(mfv))
          return mfv.DisplayName;

        var isCompiled = typeElement is ICompiledTypeElement;
        if (isCompiled)
          return mfv.CompiledName;

        return mfv.LogicalName;
      }
      catch (Exception e)
      {
        Logger.LogMessage(LoggingLevel.WARN, "Couldn't get CompiledName attribute value:");
        Logger.LogExceptionSilently(e);
      }

      return SharedImplUtil.MISSING_DECLARATION_NAME;
    }

    public static bool IsImplicitAccessor([NotNull] this FSharpMemberOrFunctionOrValue mfv)
    {
      if (mfv.IsPropertyGetterMethod)
        return mfv.CurriedParameterGroups[0].IsEmpty();

      if (mfv.IsPropertySetterMethod)
        return mfv.CurriedParameterGroups[0]?.Count == 1;

      return false;
    }

    public static FSharpFileKind GetFSharpFileKind([CanBeNull] this IPsiSourceFile sourceFile)
    {
      var fileExtension = sourceFile?.GetLocation().ExtensionNoDot;
      if (fileExtension == "fsi" || fileExtension == "mli")
        return FSharpFileKind.SigFile;

      return FSharpFileKind.ImplFile;
    }

    [CanBeNull]
    public static IDeclaredElement GetActivePatternCaseByIndex(this IFSharpDeclaration declaration, int index)
    {
      if (declaration.NameIdentifier is ActivePatternId activePatternId)
        return activePatternId.GetCase(index)?.DeclaredElement;

      return null;
    }

    public static TreeNodeCollection<IAttribute> GetAttributes([NotNull] this IDeclaration declaration)
    {
      switch (declaration)
      {
        case IFSharpTypeOldDeclaration typeDeclaration:
          return typeDeclaration.Attributes;
        case IMemberSignatureOrDeclaration memberDeclaration:
          return memberDeclaration.Attributes;
        case IFSharpPattern fsPattern:
          return fsPattern.Attributes;
        case IDeclaredModuleDeclaration moduleDeclaration:
          return moduleDeclaration.Attributes;
        default: return TreeNodeCollection<IAttribute>.Empty;
      }
    }

    public static string GetAttributeShortName([NotNull] this string attrName) =>
      attrName.TrimFromEnd(AttributeSuffix);

    public static bool IsCliMutableRecord([CanBeNull] this ITypeElement type)
    {
      if (!(type is TypeElement typeElement))
        return false;

      // todo: climutable attr can be on anon part (`type R`)
      foreach (var part in typeElement.EnumerateParts())
        if (part is IRecordPart { CliMutable: true })
          return true;

      return false;
    }

    [CanBeNull]
    public static TPart GetPart<TPart>([CanBeNull] this ITypeElement type)
      where TPart : class, IFSharpTypePart
    {
      // todo: check signature first if one is present

      if (!(type is TypeElement typeElement))
        return null;

      foreach (var part in typeElement.EnumerateParts())
        if (part is TPart expectedPart)
          return expectedPart;

      return null;
    }

    [CanBeNull]
    public static TypePart GetFirstTypePart([CanBeNull] this ITypeElement typeElement) =>
      typeElement.GetPart<IFSharpTypePart>()?.GetFirstPart();

    public static bool IsFSharpException(this ITypeElement typeElement) =>
      typeElement switch
      {
        IFSharpSourceTypeElement fsTypeElement => fsTypeElement.GetPart<IFSharpExceptionPart>() != null,
        ICompiledElement compiled when compiled.IsFromFSharpAssembly() => compiled.IsCompiledException(),
        _ => false
      };

    public static bool IsUnion([NotNull] this ITypeElement typeElement) =>
      typeElement switch
      {
        IFSharpSourceTypeElement fsTypeElement => fsTypeElement.GetPart<IUnionPart>() != null,
        IFSharpCompiledTypeElement fsCompiledTypeElement => fsCompiledTypeElement.Representation.IsUnion,
        _ => false
      };

    public static bool IsUnionCase([NotNull] this ITypeElement typeElement) =>
      typeElement switch
      {
        IFSharpSourceTypeElement fsTypeElement => fsTypeElement.GetPart<UnionCasePart>() != null,
        ICompiledElement compiled when compiled.IsFromFSharpAssembly() => compiled.IsCompiledUnionCase(),
        _ => false
      };

    [NotNull]
    public static IList<IFSharpUnionCase> GetSourceUnionCases([CanBeNull] this ITypeElement type) =>
      GetPart<IUnionPart>(type)?.Cases ?? EmptyList<IFSharpUnionCase>.Instance;

    public static string[] GetUnionCaseNames(this IFSharpTypeElement typeElement) =>
      typeElement is IFSharpCompiledTypeElement { Representation: FSharpCompiledTypeRepresentation.Union repr }
        ? repr.cases
        : GetPart<IUnionPart>(typeElement)?.CaseNames ?? EmptyArray<string>.Instance;

    [CanBeNull]
    public static FSharpUnionTagsClass GetUnionTagsClass([CanBeNull] this ITypeElement type) =>
      GetPart<IUnionPart>(type) is UnionPartBase { IsSingleCase: false } unionPart
        ? new FSharpUnionTagsClass(unionPart.TypeElement)
        : null;

    public static IFSharpParameterOwner GetGeneratedConstructor(this ITypeElement type)
    {
      if (!(type is TypeElement typeElement))
        return null;

      foreach (var part in typeElement.EnumerateParts())
        if (part is IFSharpGeneratedConstructorOwnerPart constructorOwnerPart)
          return constructorOwnerPart.GetConstructor();

      return null;
    }

    public static string GetSourceName([NotNull] this TypeElement typeElement)
    {
      foreach (var part in typeElement.EnumerateParts())
        if (part is IFSharpTypePart { SourceName: not SharedImplUtil.MISSING_DECLARATION_NAME and var name })
          return name;

      return typeElement.ShortName;
    }

    public static bool HasModuleSuffix([NotNull] this string shortName) =>
      shortName.EndsWith(ModuleSuffix, StringComparison.Ordinal);

    /// Not fully correct, since type parameter count doesn't fully follow logic used for source elements.
    /// Current implementation allows good enough results in FSharpResolveUtil.resolvesToAssociatedModule.
    public static ITypeElement TryGetAssociatedType([NotNull] this CompiledTypeElement moduleTypeElement, string sourceName)
    {
      Assertion.Assert(moduleTypeElement is FSharpCompiledModule, "moduleTypeElement.IsCompiledModule()");

      bool IsAssociatedType(ITypeElement t) =>
        !t.Equals(moduleTypeElement) && t is not FSharpCompiledModule && t.GetSourceName() == sourceName;

      ITypeElement ChooseType(ICollection<ITypeElement> typeElements)
      {
        var possiblyAssociatedTypes = typeElements.Where(IsAssociatedType).AsIList();
        if (possiblyAssociatedTypes.Count == 1)
          return possiblyAssociatedTypes[0];

        var typeWithoutTypeParams = possiblyAssociatedTypes.SingleItem(element => element.TypeParametersCount == 0);
        if (typeWithoutTypeParams != null)
          return typeWithoutTypeParams;

        return possiblyAssociatedTypes.FirstOrDefault();
      }

      var containingType = moduleTypeElement.GetContainingType();
      if (containingType != null)
        return ChooseType(containingType.NestedTypes);

      var ns = moduleTypeElement.GetContainingNamespace();
      var symbolScope = moduleTypeElement.Module.GetModuleOnlySymbolScope(false);
      return ChooseType(ns.GetNestedTypeElements(symbolScope));
    }

    [NotNull]
    private static string GetSourceName([NotNull] this ICompiledElement compiledElement)
    {
      if (compiledElement.IsFromFSharpAssembly() &&
          compiledElement.GetFirstArgValue(FSharpPredefinedType.CompilationSourceNameAttrTypeName) is string sourceName &&
          sourceName != SharedImplUtil.MISSING_DECLARATION_NAME)
        return sourceName;

      return compiledElement.ShortName;
    }

    [NotNull] public static string GetSourceName([NotNull] this IDeclaredElement declaredElement) =>
      declaredElement switch
      {
        IConstructor ctor => ctor.ContainingType?.GetSourceName() ?? ctor.ShortName,
        INamespace ns => ns.IsRootNamespace ? "global" : ns.ShortName,
        IFSharpDeclaredElement fsElement => fsElement.SourceName,
        ICompiledElement compiledElement => GetSourceName(compiledElement),
        _ => declaredElement.ShortName
      };

    public static string GetHeadPatternName([CanBeNull] this IBinding binding) =>
      binding?.HeadPattern is IReferencePat referencePat
        ? referencePat.SourceName
        : SharedImplUtil.MISSING_DECLARATION_NAME;

    public static AccessRights GetFSharpRepresentationAccessRights([CanBeNull] this ITypeElement type)
    {
      if (!(type is TypeElement typeElement))
        return AccessRights.PUBLIC;

      foreach (var part in typeElement.EnumerateParts())
        if (part is IFSharpRepresentationAccessRightsOwner accessRightsOwner)
          return accessRightsOwner.RepresentationAccessRights;
      return AccessRights.PUBLIC;
    }

    public static AccessRights GetRepresentationAccessRights([CanBeNull] this ITypeElement type)
    {
      if (!(type is TypeElement typeElement))
        return AccessRights.PUBLIC;

      foreach (var part in typeElement.EnumerateParts())
        switch (part)
        {
          case UnionCasePart casePart:
            if (casePart.Parent is IUnionPart parent && parent.RepresentationAccessRights != AccessRights.PUBLIC)
              return AccessRights.INTERNAL;
            break;

          case IFSharpRepresentationAccessRightsOwner accessRightsOwner:
            if (accessRightsOwner.RepresentationAccessRights != AccessRights.PUBLIC)
              return AccessRights.INTERNAL;
            break;
        }

      return AccessRights.PUBLIC;
    }

    // todo: hidden by signature in fsi
    public static AccessRights GetRepresentationAccessRights([NotNull] this IFSharpTypeDeclaration declaration) =>
      declaration.TypeRepresentation is ISimpleTypeRepresentation repr
        ? FSharpModifiersUtil.GetAccessRights(repr.AccessModifier)
        : AccessRights.PUBLIC;

    public static PartKind GetSimpleTypeKindFromAttributes(this IFSharpTypeDeclaration decl) =>
      GetTypeKind(decl.Attributes, out var kind) ? kind : PartKind.Class; // todo struct or class only

    public static bool GetTypeKind(TreeNodeCollection<IAttribute> attributes, out PartKind fSharpPartKind)
    {
      foreach (var attr in attributes)
        switch (attr.ReferenceName?.ShortName.DropAttributeSuffix())
        {
          case Interface:
          {
            fSharpPartKind = PartKind.Interface;
            return true;
          }

          case AbstractClass:
          case Sealed:
          case Class:
          {
            fSharpPartKind = PartKind.Class;
            return true;
          }

          case Struct:
          {
            fSharpPartKind = PartKind.Struct;
            return true;
          }
        }

      fSharpPartKind = default;
      return false;
    }

    public static PartKind GetTypeKind([NotNull] this IFSharpTypeDeclaration declaration)
    {
      if (GetTypeKind(declaration.Attributes, out var typeKind))
        return typeKind;

      var typeMembers = declaration.TypeMembersEnumerable;
      if (typeMembers.IsEmpty())
        return PartKind.Class;

      foreach (var member in typeMembers)
        if (member is not IInterfaceInherit && member is not IAbstractMemberDeclaration)
          return PartKind.Class;

      if (declaration.PrimaryConstructorDeclaration != null)
        return PartKind.Class;

      return PartKind.Interface;
    }

    [NotNull]
    public static TypeAugmentation GetTypeAugmentationInfo([NotNull] ITypeExtensionDeclaration declaration)
    {
      var extensionNameInfo =
        new NameAndParametersCount(declaration.SourceName, declaration.TypeParameterDeclarations.Count);

      var declaredTypeNames = new Dictionary<NameAndParametersCount, TypeAugmentation>();

      void RecordName(IFSharpTypeOldDeclaration typeDeclaration)
      {
        var sourceName = typeDeclaration.SourceName;
        if (sourceName == SharedImplUtil.MISSING_DECLARATION_NAME)
          return;

        // todo: should check source name only
        var compiledName = typeDeclaration.CompiledName;
        if (compiledName == SharedImplUtil.MISSING_DECLARATION_NAME)
          return;

        var parametersCount = typeDeclaration.TypeParameterDeclarations.Count;
        var augmentationInfo =
          TypeAugmentation.NewTypePart(compiledName, parametersCount, typeDeclaration.TypePartKind);

        var nameInfo = new NameAndParametersCount(sourceName, parametersCount);
        declaredTypeNames[nameInfo] = augmentationInfo;
      }

      var ownTypeDeclarationGroup = TypeDeclarationGroupNavigator.GetByTypeDeclaration(declaration);
      var moduleDeclaration = ModuleLikeDeclarationNavigator.GetByMember(ownTypeDeclarationGroup).NotNull();

      foreach (var member in moduleDeclaration.MembersEnumerable)
      {
        if (member is IExceptionDeclaration exceptionDeclaration)
        {
          RecordName(exceptionDeclaration);
          continue;
        }

        if (!(member is ITypeDeclarationGroup declarationGroup))
          continue;

        foreach (var typeDeclaration in declarationGroup.TypeDeclarations)
          if (!(typeDeclaration is ITypeExtensionDeclaration))
            RecordName(typeDeclaration);
      }

      return declaredTypeNames.TryGetValue(extensionNameInfo, out var typeAugmentation)
        ? typeAugmentation
        : TypeAugmentation.Extension;
    }

    public static string DropAttributeSuffix([NotNull] this string attrName) =>
      attrName.Length > "Attribute".Length && attrName.EndsWith("Attribute", StringComparison.Ordinal)
        ? attrName.SubstringBeforeLast(AttributeSuffix, StringComparison.Ordinal)
        : attrName;

    public static IAttribute GetAttribute(this TreeNodeCollection<IAttribute> attributes, [NotNull] string shortName)
    {
      foreach (var attr in attributes)
        if (attr.ShortNameEquals(shortName))
          return attr;

      return null;
    }

    public static bool HasAttribute(this TreeNodeCollection<IAttribute> attributes, [NotNull] string shortName) =>
      GetAttribute(attributes, shortName) != null;

    public static void ReplaceIdentifier([CanBeNull] this IFSharpIdentifier fsIdentifier, string name)
    {
      var token = fsIdentifier?.IdentifierToken;
      if (token == null)
        return;

      name = NamingManager.GetNamingLanguageService(fsIdentifier.Language).MangleNameIfNecessary(name);
      using var _ = WriteLockCookie.Create(fsIdentifier.IsPhysical());
      ModificationUtil.ReplaceChild(token, new FSharpIdentifierToken(name));
    }

    public static void AddTokenAfter([NotNull] this ITreeNode anchor, [NotNull] TokenNodeType tokenType)
    {
      using var _ = WriteLockCookie.Create(anchor.NotNull().IsPhysical());
      ModificationUtil.AddChildAfter(anchor, tokenType.CreateLeafElement());
    }

    public static void AddTokenBefore([NotNull] this ITreeNode anchor, [NotNull] TokenNodeType tokenType)
    {
      using var _ = WriteLockCookie.Create(anchor.NotNull().IsPhysical());
      ModificationUtil.AddChildBefore(anchor, tokenType.CreateLeafElement());
    }

    [CanBeNull]
    public static IDeclaredElement GetModuleToUpdateName([NotNull] this IFSharpSourceTypeElement fsTypeElement,
      [CanBeNull] string newName)
    {
      if (!(fsTypeElement is TypeElement typeElement))
        return null;

      var typeSourceName = fsTypeElement.SourceName;
      foreach (var part in typeElement.EnumerateParts())
      {
        foreach (var child in part.Parent.NotNull().Children())
        {
          if (child is not (IModulePart and TypePart typePart))
            continue;

          if (typePart.TypeElement is not IFSharpSourceTypeElement otherTypeElement)
            continue;

          var sourceName = otherTypeElement.SourceName;
          if (sourceName == typeSourceName || sourceName == newName)
            return otherTypeElement;
        }
      }

      return null;
    }

    public static bool HasAttribute([NotNull] this TypeElement typeElement, string attrShortName)
    {
      foreach (var part in typeElement.EnumerateParts())
        if (part.AttributeClassNames.Contains(attrShortName))
          return true;
      return false;
    }

    // todo: uses different logic: names in parts for modules and resolve for other type elements
    public static bool HasAutoOpenAttribute([NotNull] this ITypeElement typeElement) =>
      typeElement switch
      {
        FSharpModule fsModule => HasAttribute(fsModule, AutoOpen),
        IFSharpTypeElement and not IFSharpModule => false,
        _ => typeElement.HasAttributeInstance(FSharpPredefinedType.AutoOpenAttrTypeName, false)
      };

    public static bool IsRecord([CanBeNull] this ITypeElement typeElement) =>
      typeElement switch
      {
        IFSharpSourceTypeElement fsTypeElement => (fsTypeElement.GetPart<IRecordPart>() != null),
        ICompiledElement compiledElement when compiledElement.Module.IsFSharpAssembly() =>
          compiledElement.GetCompilationMappingFlag() == SourceConstructFlags.RecordType,
        _ => false
      };

    public static IList<string> GetRecordFieldNames([NotNull] this ITypeElement typeElement)
    {
      switch (typeElement)
      {
        case IFSharpSourceTypeElement fsTypeElement:
          return fsTypeElement.GetPart<IRecordPart>()?.Fields.Select(f => f.ShortName).AsIList() ??
                 EmptyList<string>.InstanceList;

        case ICompiledElement _:
          return typeElement.Properties.Where(p => p.IsCompiledFSharpField()).Select(p => p.ShortName).AsIList();

        default:
          return EmptyArray<string>.Instance;
      }
    }

    public static bool IsModule(this ITypeElement typeElement) => typeElement is IFSharpModule;

    public static ModuleMembersAccessKind GetAccessType([NotNull] this ITypeElement typeElement)
    {
      if (typeElement is IEnum)
        return ModuleMembersAccessKind.RequiresQualifiedAccess;

      if (typeElement is IFSharpTypeElement fsTypeElement)
        return fsTypeElement.AccessKind;

      return MayHaveRequireQualifiedAccessAttribute(typeElement) && typeElement.HasRequireQualifiedAccessAttribute()
        ? ModuleMembersAccessKind.RequiresQualifiedAccess
        : ModuleMembersAccessKind.Normal;
    }

    public static bool MayHaveRequireQualifiedAccessAttribute([NotNull] this ITypeElement typeElement) =>
      typeElement.IsModule() || typeElement.IsUnion() || typeElement.IsRecord();

    public static bool RequiresQualifiedAccess([NotNull] this ITypeElement typeElement) =>
      typeElement.GetAccessType() == ModuleMembersAccessKind.RequiresQualifiedAccess;

    public static bool IsAutoImported([NotNull] this IClrDeclaredElement declaredElement)
    {
      var psiModule = declaredElement.Module;
      var autoOpenCache = psiModule.GetSolution().GetComponent<FSharpAutoOpenCache>();
      var autoOpenedModules = autoOpenCache.GetAutoOpenedModules(psiModule);

      // todo: assembly level auto open modules
      var ns = GetNamespace(declaredElement);
      return ns != null && autoOpenedModules.Contains(ns.QualifiedName);
    }

    [CanBeNull]
    public static INamespace GetNamespace([NotNull] this IClrDeclaredElement declaredElement)
    {
      if (declaredElement is ITypeElement typeElement)
        return typeElement.GetContainingNamespace();

      if (declaredElement.GetContainingType() is { } containingType)
        return containingType.GetContainingNamespace();

      if (declaredElement is INamespace ns)
        return ns.GetContainingNamespace();

      return null;
    }

    [CanBeNull]
    private static T GetOutermostNode<T, TMatchingNode>([CanBeNull] this T node, bool singleLevel = false)
      where T : class, ITreeNode
      where TMatchingNode : class, T
    {
      if (node == null) return null;

      while (node.Parent is TMatchingNode matchingNode)
      {
        if (singleLevel)
          return matchingNode;

        node = matchingNode;
      }
      return node;
    }

    [CanBeNull]
    public static IFSharpExpression IgnoreParentParens([CanBeNull] this IFSharpExpression fsExpr,
      bool singleLevel = false, bool includingBeginEndExpr = true) =>
      includingBeginEndExpr
        ? fsExpr.GetOutermostNode<IFSharpExpression, IParenOrBeginEndExpr>(singleLevel)
        : fsExpr.GetOutermostNode<IFSharpExpression, IParenExpr>(singleLevel);

    [CanBeNull]
    public static ITypeUsage IgnoreParentParens([CanBeNull] this ITypeUsage typeUsage, bool singleLevel = false) =>
      typeUsage.GetOutermostNode<ITypeUsage, IParenTypeUsage>(singleLevel);

    public static ITreeNode IgnoreParentChameleonExpr([NotNull] this ITreeNode treeNode) =>
      treeNode.Parent is IChameleonExpression parenExpr
        ? parenExpr.Parent
        : treeNode.Parent;

    [CanBeNull]
    public static IFSharpExpression IgnoreInnerParens([CanBeNull] this IFSharpExpression fsExpr,
      bool singleLevel = false, bool includingBeginEndExpr = true)
    {
      if (fsExpr == null)
        return null;

      while (fsExpr is IParenOrBeginEndExpr { InnerExpression: { } innerExpr } &&
             (includingBeginEndExpr || fsExpr is IParenExpr))
      {
        if (singleLevel)
          return innerExpr;

        fsExpr = innerExpr;
      }
      return fsExpr;
    }

    public static IFSharpPattern IgnoreParentParens([CanBeNull] this IFSharpPattern fsPattern, bool singleLevel = false) =>
      fsPattern.GetOutermostNode<IFSharpPattern, IParenPat>(singleLevel);

    public static IFSharpPattern IgnoreInnerParens([CanBeNull] this IFSharpPattern fsPattern, bool singleLevel = false)
    {
      if (fsPattern == null)
        return null;

      while (fsPattern is IParenPat { Pattern: { } innerPat })
      {
        if (singleLevel)
          return innerPat;

        fsPattern = innerPat;
      }
      return fsPattern;
    }

    [CanBeNull]
    public static ITypeUsage IgnoreInnerParens([CanBeNull] this ITypeUsage typeUsage, bool singleLevel = false, 
      bool ignoreParameterSignature = true)
    {
      if (typeUsage == null)
        return null;

      // Ignore top-level parameter signature wrapper type usage.
      if (ignoreParameterSignature)
        typeUsage = IgnoreParameterSignature(typeUsage);

      while (typeUsage is IParenTypeUsage { InnerTypeUsage: { } innerTypeUsage })
      {
        if (singleLevel)
          return innerTypeUsage;

        typeUsage = innerTypeUsage;
      }
      return typeUsage;
    }

    [CanBeNull]
    public static ITypeUsage IgnoreParameterSignature([CanBeNull] this ITypeUsage typeUsage) =>
      typeUsage is IParameterSignatureTypeUsage parameterSignatureTypeUsage
        ? parameterSignatureTypeUsage.TypeUsage
        : typeUsage;

    [NotNull]
    public static IFSharpReferenceOwner SetName([NotNull] this IFSharpReferenceOwner referenceOwner,
      [NotNull] string name)
    {
      if (referenceOwner.NameIdentifier?.IdentifierToken is { } id)
        ModificationUtil.ReplaceChild(id, new FSharpIdentifierToken(name));

      return referenceOwner;
    }

    [NotNull]
    public static string GetQualifiedName(this IClrDeclaredElement typeOrNs)
    {
      if (typeOrNs is INamespace ns)
        return ns.QualifiedName;

      if (typeOrNs is not ITypeElement typeElement)
        throw new InvalidOperationException($"Unexpected {typeOrNs.GetType()} element");

      var builder = new StringBuilder(typeElement.GetSourceName());

      var containingType = typeOrNs.GetContainingType();
      while (containingType != null)
      {
        builder.Prepend(".");
        builder.Prepend(containingType.GetSourceName());
        containingType = containingType.GetContainingType();
      }

      var containingNs = typeElement.GetContainingNamespace();
      if (!containingNs.IsRootNamespace)
      {
        builder.Prepend(".");
        builder.Prepend(containingNs.QualifiedName);  
      }

      return builder.ToString();
    }

    [NotNull]
    public static string GetQualifiedName([CanBeNull] IReferenceName qualifier,
      [CanBeNull] IFSharpIdentifier identifier)
    {
      if (qualifier == null && identifier == null)
        return SharedImplUtil.MISSING_DECLARATION_NAME;

      if (qualifier == null)
        return identifier.Name;

      return identifier != null
        ? qualifier.QualifiedName + "." + identifier.Name
        : qualifier.QualifiedName;
    }

    public static IList<string> GetNames([CanBeNull] this IReferenceExpr referenceExpr)
    {
      var result = new List<string>();
      while (referenceExpr != null)
      {
        var shortName = referenceExpr.ShortName;
        if (shortName.IsEmpty() || shortName == SharedImplUtil.MISSING_DECLARATION_NAME)
          break;

        result.Insert(0, shortName);
        referenceExpr = referenceExpr.Qualifier as IReferenceExpr;
      }

      return result;
    }

    public static ModuleMembersAccessKind GetAccessKind(this IEnumerable<TypePart> parts) =>
      parts.Any(part => part is IFSharpTypePart { AccessKind: ModuleMembersAccessKind.RequiresQualifiedAccess })
        ? ModuleMembersAccessKind.RequiresQualifiedAccess
        : ModuleMembersAccessKind.Normal;

    public static ModuleMembersAccessKind GetAccessType(this TreeNodeEnumerable<IAttribute> attributes)
    {
      var autoOpen = false;

      foreach (var attr in attributes)
        switch (attr.ReferenceName?.ShortName.DropAttributeSuffix())
        {
          case "AutoOpen":
            autoOpen = true;
            break;
          case "RequireQualifiedAccess":
            return ModuleMembersAccessKind.RequiresQualifiedAccess;
        }

      return autoOpen
        ? ModuleMembersAccessKind.AutoOpen
        : ModuleMembersAccessKind.Normal;
    }

    public static IList<ITypeParameterDeclaration> GetTypeParameterDeclarations(
      [NotNull] this IFSharpTypeOrExtensionDeclaration decl)
    {
      var decls = decl.TypeParameterDeclarationList?.TypeParameters;
      if (decls == null)
        return EmptyList<ITypeParameterDeclaration>.Instance;

      return decls.Value.ToListWhere(typeParamDecl => !typeParamDecl.Attributes.HasAttribute("Measure"));
    }

    public static IList<ITypeParameter> GetAllTypeParametersReversed(this ITypeElement typeElement) =>
      typeElement.GetAllTypeParameters().ResultingList().Reverse();

    [CanBeNull]
    public static IDeclaredElement TryCreateOperator<TDeclaration>([NotNull] this TDeclaration decl)
      where TDeclaration : FSharpDeclarationBase, IModifiersOwnerDeclaration, ITypeMemberDeclaration
    {
      var name = decl.DeclaredName;
      if (!name.StartsWith("op_", StringComparison.Ordinal))
        return null;

      switch (name)
      {
        case StandardOperatorNames.Explicit:
          return new FSharpConversionOperator<TDeclaration>(decl, true);
        case StandardOperatorNames.Implicit:
          return new FSharpConversionOperator<TDeclaration>(decl, false);
        default:
          return new FSharpSignOperator<TDeclaration>(decl);
      }
    }

    public static IDeclaredElement GetOrCreateDeclaredElement<T>(this T decl, Func<T, IDeclaredElement> factory)
      where T : ICachedTypeMemberDeclaration
    {
      decl.AssertIsValid("Asking declared element from invalid declaration");
      var cache = decl.GetPsiServices().Caches.SourceDeclaredElementsCache;
      // todo: calc types on demand in members (move cookie to FSharpTypesUtil)
      using (CompilationContextCookie.GetOrCreate(decl.GetPsiModule().GetContextFromModule()))
        return cache.GetOrCreateDeclaredElement(decl, factory);
    }

    public static IDeclaredElement CreateMethod([NotNull] this IMemberSignatureOrDeclaration decl)
    {
      var compiledName = decl.CompiledName;
      if (compiledName.StartsWith("op_", StringComparison.Ordinal) && decl.IsStatic)
        return compiledName switch
        {
          StandardOperatorNames.Explicit => new FSharpConversionOperator<IMemberSignatureOrDeclaration>(decl, true),
          StandardOperatorNames.Implicit => new FSharpConversionOperator<IMemberSignatureOrDeclaration>(decl, false),
          _ => new FSharpSignOperator<IMemberSignatureOrDeclaration>(decl)
        };

      return new FSharpMethod<IMemberSignatureOrDeclaration>(decl);
    }

    public static IDeclaredElement CreateMemberDeclaredElement([NotNull] this IMemberSignatureOrDeclaration decl, FSharpSymbol fcsSymbol)
    {
      if (!(fcsSymbol is FSharpMemberOrFunctionOrValue mfv)) return null;

      if (mfv.IsProperty) return CreateProperty(decl, mfv);

      var property = mfv.AccessorProperty?.Value;
      if (property != null)
      {
        var cliEvent = property.EventForFSharpProperty?.Value;
        return cliEvent != null
          ? new FSharpCliEvent<IMemberSignatureOrDeclaration>(decl)
          : CreateProperty(decl, property);
      }

      return new FSharpMethod<IMemberSignatureOrDeclaration>(decl);
    }

    [NotNull]
    public static IDeclaredElement CreateProperty([NotNull] this IMemberSignatureOrDeclaration decl,
      FSharpMemberOrFunctionOrValue mfv)
    {
      foreach (var accessor in decl.AccessorDeclarationsEnumerable)
        if (accessor.IsExplicit)
          return decl.IsIndexer
            ? new FSharpIndexerProperty(decl)
            : new FSharpPropertyWithExplicitAccessors(decl);

      return new FSharpProperty<IMemberSignatureOrDeclaration>(decl, mfv);
    }

    [CanBeNull]
    public static IAccessorDeclaration TryGet(this IEnumerable<IAccessorDeclaration> accessors, AccessorKind kind)
    {
      foreach (var accessor in accessors)
        if (accessor.Kind == kind)
          return accessor;
      return null;
    }

    public static XmlNode GetXmlDoc(this IFSharpSourceTypeElement typeElement, bool inherit) =>
      typeElement.GetFirstTypePart()?.GetDeclaration()?.GetXMLDoc(inherit);

    private static string Print(AccessRights accessRights) => accessRights.ToString().ToLowerInvariant();

    internal static string TestToString(this IFSharpSourceTypeElement typeElement, string typeParameterString)
    {
      IEnumerable<string> GetModifiers()
      {
        var sourceName = typeElement.SourceName;
        if (sourceName != typeElement.ShortName)
          yield return sourceName;

        var accessRights = typeElement.GetFSharpAccessRights();
        if (accessRights is not { IsFilePrivate: false, AccessRights: AccessRights.PUBLIC })
          yield return $"{accessRights}, compiled: {Print(typeElement.GetAccessRights())}";

        if (typeElement is ILanguageSpecificDeclaredElement { IsErased: true })
          yield return "erased";
      }

      var stringBuilder = new StringBuilder();
      stringBuilder.Append($"{typeElement.GetType().Name}:");
      stringBuilder.Append(typeElement.IsValid() ? typeElement.GetClrName().FullName : "<Invalid>");

      stringBuilder.Append(typeParameterString);

      var list = GetModifiers().ToList();
      if (!list.IsEmpty())
        stringBuilder.Append($" ({list.Join(", ")})");

      return stringBuilder.ToString();
    }

    internal static IEnumerable<string> GetTestFSharpTypePartModifiers(this IFSharpTypePart typePart)
    {
      var sourceName = typePart.SourceName;
      if (sourceName != typePart.ShortName)
        yield return sourceName;

      var accessRights = typePart.SourceAccessRights;
      if (accessRights is not AccessRights.PUBLIC)
        yield return Print(accessRights);
    }

    public static ITypeDeclaration GetDefiningDeclaration(this IFSharpSourceTypeElement typeElement)
    {
      TypePart currentDefiningPart = null;
      foreach (var part in typeElement.EnumerateParts())
      {
        if (currentDefiningPart == null)
          currentDefiningPart = part;
        else
        {
          var currentPartFilePart = currentDefiningPart.GetRoot() as FSharpProjectFilePart;
          var partFilePart = part.GetRoot() as FSharpProjectFilePart;

          if (currentPartFilePart != partFilePart && currentPartFilePart != null && partFilePart != null)
          {
            var fcsProjectProvider = typeElement.Module.GetSolution().GetComponent<IFcsProjectProvider>();
            var currentPartSourceFileIndex = fcsProjectProvider.GetFileIndex(currentPartFilePart.SourceFile);
            var partSourceFileIndex = fcsProjectProvider.GetFileIndex(partFilePart.SourceFile);
            if (partSourceFileIndex < currentPartSourceFileIndex)
              currentDefiningPart = part;
          }
          else
          {
            if (part.Offset < currentDefiningPart.Offset)
              currentDefiningPart = part;
          }
        }
      }

      return currentDefiningPart?.GetDeclaration() as ITypeDeclaration;
    }
  }
}
