using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Naming;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.Extension;
using JetBrains.Util.Logging;
using Microsoft.FSharp.Core;
using PrettyNaming = FSharp.Compiler.PrettyNaming;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public static class FSharpImplUtil
  {
    public const string CompiledNameAttrName = "Microsoft.FSharp.Core.CompiledNameAttribute";
    public const string ModuleSuffix = "CompilationRepresentationFlags.ModuleSuffix";
    public const string CompiledName = "CompiledName";
    public const string AttributeSuffix = "Attribute";
    public const string Interface = "Interface";
    public const string AbstractClass = "AbstractClass";
    public const string Class = "Class";
    public const string Sealed = "Sealed";
    public const string Struct = "Struct";

    public static string GetShortName([NotNull] this IAttribute attr) =>
      attr.ReferenceName?.ShortName.GetAttributeShortName();

    public static bool ShortNameEquals([NotNull] this IAttribute attr, [NotNull] string shortName) =>
      attr.GetShortName() == shortName;

    [CanBeNull]
    private static FSharpString GetStringConst([CanBeNull] ISynExpr expr)
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

    private static bool IsModuleSuffixExpr([CanBeNull] ISynExpr expr)
    {
      switch (expr)
      {
        case ParenExpr parenExpr:
          return IsModuleSuffixExpr(parenExpr.InnerExpression);
        case IReferenceExpr referenceExpr:
          return referenceExpr.QualifiedName == ModuleSuffix;
      }

      return false;
    }

    private static bool IsModuleSuffixAttribute([NotNull] this IAttribute attr) =>
      attr.ShortNameEquals("CompilationRepresentation") && IsModuleSuffixExpr(attr.Expression);

    public static string GetModuleCompiledName([CanBeNull] this IIdentifier identifier,
      TreeNodeCollection<IAttribute> attributes)
    {
      var hasModuleSuffix = false;
      string compiledName = null;

      foreach (var attr in attributes)
      {
        if (GetCompiledNameValue(attr, out var value))
          compiledName = value;

        if (!attr.IsModuleSuffixAttribute())
          continue;

        hasModuleSuffix = true;
        break;
      }

      var sourceName = identifier?.Name;
      return hasModuleSuffix && sourceName != null
        ? sourceName + "Module"
        : compiledName ?? sourceName ?? SharedImplUtil.MISSING_DECLARATION_NAME;
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

      if (identifier is IFSharpIdentifierLikeNode fsIdentifier &&
          fsIdentifier.IdentifierToken?.GetTokenType() == FSharpTokenType.SYMBOLIC_OP)
        return PrettyNaming.CompileOpName.Invoke(name);

      return name;
    }

    [NotNull]
    public static string GetSourceName([CanBeNull] this IIdentifier identifier) =>
      identifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public static string GetSourceName([CanBeNull] this ITreeNode treeNode) =>
      GetSourceName(treeNode as IIdentifier);

    public static TreeTextRange GetNameRange([CanBeNull] this IFSharpIdentifierLikeNode identifier)
    {
      if (identifier == null)
        return TreeTextRange.InvalidRange;

      if (identifier is IActivePatternId activePatternId)
        return activePatternId.GetCasesRange();

      var nameRange = identifier.GetTreeTextRange();
      var identifierToken = identifier.IdentifierToken;
      if (identifierToken == null)
        return nameRange;

      return identifierToken.GetText().IsEscapedWithBackticks()
        ? nameRange.TrimLeft(2).TrimRight(2)
        : nameRange;
    }

    public static TreeTextRange GetNameIdentifierRange([CanBeNull] this IFSharpIdentifierLikeNode identifier) =>
      identifier?.GetTreeTextRange() ?? TreeTextRange.InvalidRange;

    /// <summary>
    /// Get name and qualifiers without backticks. Qualifiers added if the token is in ILongIdentifier.
    /// </summary>
    [NotNull]
    public static string[] GetQualifiersAndName(this FSharpIdentifierToken token)
    {
      if (!(token.Parent is ILongIdentifier longIdentifier))
        return new[] {token.GetText().RemoveBackticks()};

      var names = new FrugalLocalHashSet<string>();
      foreach (var id in longIdentifier.IdentifiersEnumerable)
      {
        names.Add(id.GetText().RemoveBackticks());
        if (id == token) break;
      }

      return names.ToArray();
    }

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
      if (typeDeclaration?.TypeParameters.Count > 0)
        clrName.Append("`" + typeDeclaration.TypeParameters.Count);

      return clrName.ToString();
    }

    [NotNull]
    public static string GetMfvCompiledName([NotNull] this FSharpMemberOrFunctionOrValue mfv)
    {
      try
      {
        var compiledNameAttr = mfv.Attributes.TryFindAttribute(CompiledNameAttrName);
        var compiledName = compiledNameAttr != null && !compiledNameAttr.Value.ConstructorArguments.IsEmpty()
          ? compiledNameAttr.Value.ConstructorArguments[0].Item2 as string
          : null;
        return compiledName ??
               (mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod
                 ? mfv.DisplayName
                 : mfv.LogicalName);
      }
      catch (Exception e)
      {
        Logger.LogMessage(LoggingLevel.WARN, "Couldn't get CompiledName attribute value:");
        Logger.LogExceptionSilently(e);
      }

      return SharedImplUtil.MISSING_DECLARATION_NAME;
    }

    public static FSharpFileKind GetFSharpFileKind([CanBeNull] this IPsiSourceFile sourceFile)
    {
      var fileExtension = sourceFile?.GetLocation().ExtensionNoDot;
      if (fileExtension == "fsi" || fileExtension == "mli")
        return FSharpFileKind.SigFile;

      return FSharpFileKind.ImplFile;
    }

    [CanBeNull]
    internal static IDeclaredElement GetActivePatternByIndex(this IFSharpDeclaration declaration, int index)
    {
      if (!(declaration.NameIdentifier is ActivePatternId patternId))
        return null;

      var cases = patternId.Cases;
      if (index < 0 || index >= cases.Count)
        return null;

      var caseDeclaration = cases[index] as IActivePatternNamedCaseDeclaration;
      return caseDeclaration?.DeclaredElement;
    }

    public static TreeNodeCollection<IAttribute> GetAttributes([NotNull] this IDeclaration declaration)
    {
      switch (declaration)
      {
        case IFSharpTypeDeclaration typeDeclaration:
          return typeDeclaration.Attributes;
        case IMemberDeclaration memberDeclaration:
          return memberDeclaration.Attributes;
        case ISynPat pat:
          return pat.Attributes;
        case IDeclaredModuleDeclaration moduleDeclaration:
          return moduleDeclaration.Attributes;
        default: return TreeNodeCollection<IAttribute>.Empty;
      }
    }

    public static string GetAttributeShortName([NotNull] this string attrName) =>
      attrName.SubstringBeforeLast("Attribute", StringComparison.Ordinal);

    public static bool IsCliMutableRecord([CanBeNull] this ITypeElement type)
    {
      if (!(type is TypeElement typeElement))
        return false;

      // todo: climutable attr can be on anon part (`type R`)
      foreach (var part in typeElement.EnumerateParts())
        if (part is IRecordPart recordPart && recordPart.CliMutable)
          return true;

      return false;
    }

    [CanBeNull]
    public static TPart GetPart<TPart>([CanBeNull] this ITypeElement type)
      where TPart : class, ClassLikeTypeElement.IClassLikePart
    {
      // todo: check signature first if one is present

      if (!(type is TypeElement typeElement))
        return null;

      foreach (var part in typeElement.EnumerateParts())
        if (part is TPart expectedPart)
          return expectedPart;

      return null;
    }

    public static bool IsException(this ITypeElement type) =>
      GetPart<IExceptionPart>(type) != null;
    
    public static bool IsUnion([NotNull] this ITypeElement type) =>
      GetPart<IUnionPart>(type) != null;

    public static bool IsUnionCase([NotNull] this ITypeElement type) =>
      GetPart<UnionCasePart>(type) != null;

    [NotNull]
    public static IList<IUnionCase> GetUnionCases([CanBeNull] this ITypeElement type) =>
      GetPart<IUnionPart>(type)?.Cases ?? EmptyList<IUnionCase>.Instance;

    [CanBeNull]
    public static FSharpUnionTagsClass GetUnionTagsClass([CanBeNull] this ITypeElement type) =>
      GetPart<IUnionPart>(type) is UnionPartBase unionPart && !unionPart.IsSingleCaseUnion
        ? new FSharpUnionTagsClass(unionPart.TypeElement)
        : null;

    public static IParametersOwner GetGeneratedConstructor(this ITypeElement type)
    {
      if (type is IGeneratedConstructorOwner constructorOwner)
        return constructorOwner.GetConstructor();

      if (!(type is TypeElement typeElement))
        return null;

      foreach (var part in typeElement.EnumerateParts())
      {
        if (part is IGeneratedConstructorOwner constructorOwnerPart)
          return constructorOwnerPart.GetConstructor();
      }

      return null;
    }

    public static string GetSourceName([NotNull] this TypeElement typeElement)
    {
      foreach (var part in typeElement.EnumerateParts())
        if (part is IFSharpTypePart fsTypePart)
          return fsTypePart.SourceName;

      return typeElement.ShortName;
    }

    public static string GetSourceName([NotNull] this IDeclaredElement declaredElement) =>
      declaredElement is IFSharpDeclaredElement fsElement
        ? fsElement.SourceName
        : declaredElement.ShortName;

    public static AccessRights GetFSharpRepresentationAccessRights([CanBeNull] this ITypeElement type)
    {
      if (!(type is TypeElement typeElement))
        return AccessRights.PUBLIC;

      foreach (var part in typeElement.EnumerateParts())
        if (part is IRepresentationAccessRightsOwner accessRightsOwner)
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
          case IUnionPart unionPart:
            if (unionPart.RepresentationAccessRights != AccessRights.PUBLIC)
              return AccessRights.INTERNAL;
            break;

          case UnionCasePart casePart:
            if (casePart.Parent is IUnionPart parent && parent.RepresentationAccessRights != AccessRights.PUBLIC)
              return AccessRights.INTERNAL;
            break;
        }

      return AccessRights.PUBLIC;
    }

    public static bool GetTypeKind(IEnumerable<IAttribute> attributes, out PartKind fSharpPartKind)
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

    [NotNull]
    public static TypeAugmentation IsTypePartDeclaration([NotNull] ITypeExtensionDeclaration extensionDeclaration)
    {
      var extNameInfo =
        new NameAndParametersCount(extensionDeclaration.SourceName, extensionDeclaration.TypeParameters.Count);

      var declaredTypeNames = new Dictionary<NameAndParametersCount, TypeAugmentation>();
      var moduleDeclaration = extensionDeclaration.GetContainingNode<IModuleLikeDeclaration>()
        .NotNull();

      foreach (var member in moduleDeclaration.MembersEnumerable)
      {
        if (member is ITypeExtensionDeclaration || !(member is IFSharpTypeDeclaration declaration))
          continue;

        var sourceName = declaration.SourceName;
        if (sourceName == SharedImplUtil.MISSING_DECLARATION_NAME)
          continue;

        var compiledName = declaration.CompiledName;
        if (compiledName == SharedImplUtil.MISSING_DECLARATION_NAME)
          continue;

        var parametersCount = declaration.TypeParameters.Count;
        var augmentationInfo = TypeAugmentation.NewTypePart(compiledName, parametersCount, declaration.TypePartKind);

        var nameInfo = new NameAndParametersCount(sourceName, parametersCount);
        declaredTypeNames[nameInfo] = augmentationInfo;
      }

      return declaredTypeNames.TryGetValue(extNameInfo, out var typeAugmentation)
        ? typeAugmentation
        : TypeAugmentation.Extension;
    }

    public static string DropAttributeSuffix([NotNull] this string attrName) =>
      attrName.SubstringBeforeLast(AttributeSuffix, StringComparison.Ordinal);

    public static bool HasAttribute(this TreeNodeCollection<IAttribute> attributes, [NotNull] string shortName)
    {
      foreach (var attr in attributes)
        if (attr.ShortNameEquals(shortName))
          return true;

      return false;
    }
    
    public static bool HasAttribute([NotNull] this IFSharpTypeDeclaration typeDeclaration, [NotNull] string shortName) =>
      HasAttribute(typeDeclaration.Attributes, shortName);

    public static void ReplaceIdentifier([CanBeNull] this IFSharpIdentifierLikeNode fsIdentifier, string name)
    {
      var token = fsIdentifier?.IdentifierToken;
      if (token == null)
        return;

      name = NamingManager.GetNamingLanguageService(fsIdentifier.Language).MangleNameIfNecessary(name);
      using (WriteLockCookie.Create(fsIdentifier.IsPhysical()))
        LowLevelModificationUtil.ReplaceChildRange(token, token, new FSharpIdentifierToken(name));
    }

    public static void AddModifierTokenAfter([NotNull] this ITreeNode anchor, [NotNull] TokenNodeType tokenType)
    {
      using var _ = WriteLockCookie.Create(anchor.NotNull().IsPhysical());
      anchor =
        anchor.NextSibling is Whitespace space
          ? ModificationUtil.ReplaceChild(space, new Whitespace())
          : ModificationUtil.AddChildAfter(anchor, new Whitespace());

      var addSpaceAfter = anchor.NextSibling?.GetTokenType() != FSharpTokenType.NEW_LINE;

      anchor = ModificationUtil.AddChildAfter(anchor, tokenType.CreateLeafElement());
      if (addSpaceAfter)
        ModificationUtil.AddChildAfter(anchor, new Whitespace());
    }

    public static IList<ITypeElement> ToTypeElements(this IList<IClrTypeName> names, IPsiModule psiModule)
    {
      var result = new List<ITypeElement>(names.Count);
      foreach (var clrTypeName in names)
      {
        if (clrTypeName == null)
          continue;

        var typeElement = TypeFactory.CreateTypeByCLRName(clrTypeName, psiModule).GetTypeElement();
        if (typeElement != null)
          result.Add(typeElement);
      }

      return result;
    }

    [CanBeNull]
    public static IDeclaredElement GetModuleToUpdateName([NotNull] this IFSharpTypeElement fsTypeElement,
      [CanBeNull] string newName)
    {
      if (!(fsTypeElement is TypeElement typeElement))
        return null;

      var typeSourceName = fsTypeElement.SourceName;
      foreach (var part in typeElement.EnumerateParts())
      {
        foreach (var child in part.Parent.NotNull("part.Parent != null").Children())
        {
          if (!(child is IModulePart && child is TypePart typePart))
            continue;

          if (!(typePart.TypeElement is IFSharpTypeElement otherTypeElement))
            continue;

          var sourceName = otherTypeElement.SourceName;
          if (sourceName == typeSourceName || sourceName == newName)
            return otherTypeElement;
        }
      }

      return null;
    }

    public static bool IsRecord([NotNull] this ITypeElement typeElement)
    {
      switch (typeElement)
      {
        case IFSharpTypeElement fsTypeElement:
          return fsTypeElement.GetPart<IRecordPart>() != null;
        case ICompiledElement compiledElement when compiledElement.Module.IsFSharpAssembly():
          return compiledElement.GetCompilationMappingFlag() == SourceConstructFlags.RecordType;
        default:
          return false;
      }
    }

    public static ICollection<string> GetRecordFieldNames([NotNull] this ITypeElement typeElement)
    {
      switch (typeElement)
      {
        case IFSharpTypeElement fsTypeElement:
          return fsTypeElement.GetPart<IRecordPart>()?.Fields.Select(f => f.ShortName).AsCollection() ??
                 EmptyList<string>.InstanceList;

        case ICompiledElement _:
          return typeElement.Properties.Where(p => p.IsCompiledFSharpField()).Select(p => p.ShortName).AsCollection();

        default:
          return EmptyArray<string>.Instance;
      }
    }

    public static ISynExpr IgnoreParentParens([NotNull] this ISynExpr synExpr)
    {
      while (synExpr.Parent is IParenExpr parenExpr)
        synExpr = parenExpr;
      return synExpr;
    }
    
    [CanBeNull]
    public static ISynExpr IgnoreInnerParens([CanBeNull] this ISynExpr synExpr)
    {
      if (synExpr == null)
        return null;

      while (synExpr is IParenExpr parenExpr && parenExpr.InnerExpression != null)
        synExpr = parenExpr.InnerExpression;
      return synExpr;
    }
    
    public static ISynPat IgnoreParentParens([CanBeNull] this ISynPat synPat)
    {
      if (synPat == null)
        return null;

      while (synPat.Parent is IParenPat parenPat)
        synPat = parenPat;
      return synPat;
    }

    public static ISynPat IgnoreInnerParens([CanBeNull] this ISynPat synPat)
    {
      if (synPat == null)
        return null;

      while (synPat is IParenPat parenPat && parenPat.Pattern != null)
        synPat = parenPat.Pattern;
      return synPat;
    }
    
    [NotNull]
    public static IFSharpReferenceOwner SetName([NotNull] this IFSharpReferenceOwner referenceOwner, 
      [NotNull] string name)
    {
      if (referenceOwner.IdentifierToken is var id && id != null)
        LowLevelModificationUtil.ReplaceChildRange(id, id, new FSharpIdentifierToken(name));

      return referenceOwner;
    }

    [NotNull]
    public static string GetQualifiedName([NotNull] this IReferenceName referenceName)
    {
      var qualifier = referenceName.Qualifier;
      var shortName = referenceName.ShortName;

      return qualifier == null
        ? shortName
        : qualifier.QualifiedName + "." + shortName;
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

    public static IList<string> GetNames([CanBeNull] this IReferenceName referenceName)
    {
      var result = new List<string>();
      while (referenceName != null)
      {
        var shortName = referenceName.ShortName;
        if (shortName.IsEmpty() || shortName == SharedImplUtil.MISSING_DECLARATION_NAME)
          break;

        result.Insert(0, shortName);
        referenceName = referenceName.Qualifier;
      }

      return result;
    }

    public static bool IsAutoOpen([CanBeNull] this IDeclaredModuleDeclaration moduleDeclaration)
    {
      if (moduleDeclaration == null)
        return false;

      foreach (var attr in moduleDeclaration.AttributesEnumerable)
        if (attr.ReferenceName?.ShortName.DropAttributeSuffix() == "AutoOpen")
          return true;

      return false;
    }
  }
}
