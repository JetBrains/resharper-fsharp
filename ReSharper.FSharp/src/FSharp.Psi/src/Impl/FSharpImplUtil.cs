using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Naming;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.Logging;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Compiler.SourceCodeServices;

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

    public static TreeTextRange GetNameRange([CanBeNull] this ILongIdentifier longIdentifier)
    {
      if (longIdentifier == null) return TreeTextRange.InvalidRange;

      var ids = longIdentifier.Identifiers;
      return ids.IsEmpty ? TreeTextRange.InvalidRange : ids.Last().GetTreeTextRange();
    }

    public static string GetShortName([NotNull] this IFSharpAttribute attr) =>
      attr.LongIdentifier?.Name.GetAttributeShortName();

    public static bool ShortNameEquals([NotNull] this IFSharpAttribute attr, [NotNull] string shortName) =>
      attr.GetShortName() == shortName;

    private static bool GetCompiledNameValue(IFSharpAttribute attr, out string compiledName)
    {
      // todo: proper expressions evaluation, e.g. "S1" + "S2"
      if (!attr.ShortNameEquals(CompiledName) || attr.ArgExpression.String == null)
      {
        compiledName = null;
        return false;
      }

      compiledName =
        attr.ArgExpression.String.GetText()
          .Substring(1, attr.ArgExpression.String.GetText().Length - 2)
          .SubstringBeforeLast("`", StringComparison.Ordinal);
      return true;
    }

    private static bool IsModuleSuffixAttribute([NotNull] this IFSharpAttribute attr) =>
      attr.ShortNameEquals("CompilationRepresentation") &&
      attr.ArgExpression.LongIdentifier?.QualifiedName == ModuleSuffix;

    public static string GetModuleCompiledName([CanBeNull] this IIdentifier identifier,
      TreeNodeCollection<IFSharpAttribute> attributes)
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


    [NotNull]
    public static string GetCompiledName([CanBeNull] this IIdentifier identifier,
      TreeNodeCollection<IFSharpAttribute> attributes)
    {
      foreach (var attr in attributes)
        if (GetCompiledNameValue(attr, out var value))
          return value;

      return identifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;
    }

    [NotNull]
    public static string GetSourceName([CanBeNull] this IIdentifier identifier)
    {
      return identifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;
    }

    public static TreeTextRange GetNameRange([CanBeNull] this IFSharpIdentifier identifier)
    {
      return identifier?.GetTreeTextRange() ?? TreeTextRange.InvalidRange;
    }

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
      clrName.Append(declaration.DeclaredName);

      var typeDeclaration = declaration as IFSharpTypeDeclaration;
      if (typeDeclaration?.TypeParameters.Count > 0)
        clrName.Append("`" + typeDeclaration.TypeParameters.Count);

      return clrName.ToString();
    }

    [NotNull]
    public static string GetMemberCompiledName([NotNull] this FSharpMemberOrFunctionOrValue mfv)
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
    internal static IDeclaredElement GetActivePatternByIndex(this IDeclaration declaration, int index)
    {
      return null;
//      var letDecl = declaration as PatternDeclarationBase;
//      var cases = letDecl?.Identifier.Children<ActivePatternCaseDeclaration>().AsIList();
//      return cases?.Count > index ? cases[index].DeclaredElement : null;
    }

    public static TreeNodeCollection<IFSharpAttribute> GetAttributes([NotNull] this IDeclaration declaration)
    {
      switch (declaration)
      {
        case IFSharpTypeDeclaration typeDeclaration:
          return typeDeclaration.Attributes;
        case IMemberDeclaration memberDeclarationm:
          return memberDeclarationm.Attributes;
//        case IPatternDeclaration letBinding:
//          return letBinding.Attributes;
        case IModuleLikeDeclaration moduleLikeDeclaration:
          return moduleLikeDeclaration.Attributes;
        default: return TreeNodeCollection<IFSharpAttribute>.Empty;
      }
    }

    public static string GetAttributeShortName([NotNull] this string attrName) =>
      attrName.SubstringBeforeLast("Attribute", StringComparison.Ordinal);

    public static bool IsCliMutableRecord([NotNull] this TypeElement typeElement)
    {
      // todo: climutable attr can be on anon part (`type R`)
      foreach (var part in typeElement.EnumerateParts())
        if (part is IRecordPart recordPart && recordPart.CliMutable)
          return true;

      return false;
    }

    public static bool IsUnion([NotNull] this TypeElement typeElement)
    {
      foreach (var part in typeElement.EnumerateParts())
        if (part is IUnionPart)
          return true;
      return false;
    }

    public static AccessRights GetFSharpRepresentationAccessRights([NotNull] this TypeElement typeElement)
    {
      foreach (var part in typeElement.EnumerateParts())
        if (part is IRepresentationAccessRightsOwner accessRightsOwner)
          return accessRightsOwner.RepresentationAccessRights;
      return AccessRights.PUBLIC;
    }

    public static AccessRights GetRepresentationAccessRights([NotNull] this TypeElement typeElement)
    {
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

    public static bool GetTypeKind(IEnumerable<IFSharpAttribute> attributes, out FSharpPartKind fSharpPartKind)
    {
      foreach (var attr in attributes)
      {
        var attrIds = attr.LongIdentifier.Identifiers;
        if (attrIds.IsEmpty)
          continue;

        switch (attrIds.LastOrDefault()?.GetText().DropAttributeSuffix())
        {
          case Interface:
          {
            fSharpPartKind = FSharpPartKind.Interface;
            return true;
          }
          case AbstractClass:
          case Sealed:
          case Class:
          {
            fSharpPartKind = FSharpPartKind.Class;
            return true;
          }
          case Struct:
          {
            fSharpPartKind = FSharpPartKind.Struct;
            return true;
          }
        }
      }

      fSharpPartKind = default;
      return false;
    }

    public static string DropAttributeSuffix([NotNull] this string attrName) =>
      attrName.SubstringBeforeLast(AttributeSuffix, StringComparison.Ordinal);

    public static bool HasAttribute([NotNull] this IFSharpTypeDeclaration typeDeclaration, [NotNull] string shortName)
    {
      foreach (var attr in typeDeclaration.Attributes)
        if (attr.ShortNameEquals(shortName))
          return true;

      return false;
    }

    public static void ReplaceIdentifier([CanBeNull] this IFSharpIdentifier fsIdentifier, string name)
    {
      var token = fsIdentifier?.IdentifierToken;
      if (token == null)
        return;

      name = NamingManager.GetNamingLanguageService(fsIdentifier.Language).MangleNameIfNecessary(name);
      using (WriteLockCookie.Create(fsIdentifier.IsPhysical()))
        LowLevelModificationUtil.ReplaceChildRange(token, token, new FSharpIdentifierToken(name));
    }
  }
}
