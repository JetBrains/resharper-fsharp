using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Logging;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public static class FSharpImplUtil
  {
    private const string CompiledNameAttrName = "Microsoft.FSharp.Core.CompiledNameAttribute";

    public static TreeTextRange GetNameRange([CanBeNull] this ILongIdentifier longIdentifier)
    {
      if (longIdentifier == null) return TreeTextRange.InvalidRange;

      // ReSharper disable once TreeNodeEnumerableCanBeUsedTag
      var ids = longIdentifier.Identifiers;
      return ids.IsEmpty ? TreeTextRange.InvalidRange : ids.Last().GetTreeTextRange();
    }

    public static bool ShortNameEquals([NotNull] this IFSharpAttribute attr, [NotNull] string shortName) =>
      attr.LongIdentifier?.Name.GetAttributeShortName() == shortName;

    [NotNull]
    public static string GetCompiledName([CanBeNull] this IIdentifier identifier,
      TreeNodeCollection<IFSharpAttribute> attributes)
    {
      var hasModuleSuffix = false;

      foreach (var attr in attributes)
      {
        // todo: proper expressions evaluation, e.g. "S1" + "S2"
        if (attr.ShortNameEquals("CompiledName") && attr.ArgExpression.String != null)
        {
          var compiledNameString = attr.ArgExpression.String.GetText();
          return compiledNameString
            .Substring(1, compiledNameString.Length - 2)
            .SubstringBeforeLast("`", StringComparison.Ordinal);
        }

        if (hasModuleSuffix || !attr.ShortNameEquals("CompilationRepresentation")) continue;
        var arg = attr.ArgExpression.LongIdentifier?.QualifiedName;
        if (arg != null && arg == "CompilationRepresentationFlags.ModuleSuffix")
          hasModuleSuffix = true;
      }
      var sourceName = identifier?.Name;
      var compiledName = hasModuleSuffix && sourceName != null ? sourceName + "Module" : sourceName;
      return compiledName ?? SharedImplUtil.MISSING_DECLARATION_NAME;
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
        return new[] {FSharpNamesUtil.RemoveBackticks(token.GetText())};

      var names = new FrugalLocalHashSet<string>();
      foreach (var id in longIdentifier.IdentifiersEnumerable)
      {
        names.Add(FSharpNamesUtil.RemoveBackticks(id.GetText()));
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
        var compiledNameAttr = mfv.Attributes.FirstOrDefault(CompiledNameAttrName);
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
      var letDecl = declaration as Let;
      var cases = letDecl?.Identifier.Children<ActivePatternCaseDeclaration>().AsIList();
      return cases?.Count > index ? cases[index].DeclaredElement : null;
    }

    [NotNull]
    public static string GetNestedModuleShortName(this INestedModuleDeclaration declaration, ICacheBuilder cacheBuilder)
    {
      var parent = declaration.Parent as IModuleLikeDeclaration;
      var shortName = declaration.ShortName;
      var moduleName =
        parent?.Children<IFSharpTypeDeclaration>().Any(t => t.TypeParameters.IsEmpty && t.ShortName.Equals(shortName, StringComparison.Ordinal)) ?? false
          ? shortName + "Module"
          : shortName;
      return cacheBuilder.Intern(moduleName);
    }

    public static TreeNodeCollection<IFSharpAttribute> GetAttributes([NotNull] this IDeclaration declaration)
    {
      switch (declaration)
      {
        case IFSharpTypeDeclaration typeDeclaration:
          return typeDeclaration.Attributes;
        case IMemberDeclaration memberDeclarationm:
          return memberDeclarationm.Attributes;
        case ILet letBinding:
          return letBinding.Attributes;
        case IModuleLikeDeclaration moduleLikeDeclaration:
          return moduleLikeDeclaration.Attributes;
        default: return TreeNodeCollection<IFSharpAttribute>.Empty;
      }
    }

    public static string GetAttributeShortName([NotNull] this string attrName) =>
      attrName.SubstringBeforeLast("Attribute", StringComparison.Ordinal);
  }
}