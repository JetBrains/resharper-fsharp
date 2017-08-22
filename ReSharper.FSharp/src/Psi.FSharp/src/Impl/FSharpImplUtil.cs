using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
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

    [CanBeNull]
    public static ITokenNode GetNameToken([CanBeNull] this ILongIdentifier longIdentifier)
    {
      if (longIdentifier == null) return null;

      var ids = longIdentifier.Identifiers;
      return ids.IsEmpty ? null : ids.Last();
    }

    private static bool AttributeHasShortName([NotNull] this IFSharpAttribute attr, [NotNull] string shortName)
    {
      var attrName = attr.LongIdentifier?.Name.SubstringBeforeLast("Attribute", StringComparison.Ordinal);
      return attrName != null && attrName.Equals(shortName, StringComparison.Ordinal);
    }

    [NotNull]
    public static string GetCompiledName([CanBeNull] IIdentifier identifier,
      TreeNodeCollection<IFSharpAttribute> attributes)
    {
      var hasModuleSuffix = false;

      foreach (var attr in attributes)
      {
        // todo: proper expressions evaluation, e.g. "S1" + "S2"
        if (attr.AttributeHasShortName("CompiledName") && attr.ArgExpression.String != null)
        {
          var compiledNameString = attr.ArgExpression.String.GetText();
          return compiledNameString.Substring(1, compiledNameString.Length - 2);
        }

        if (hasModuleSuffix || !attr.AttributeHasShortName("CompilationRepresentation")) continue;
        var arg = attr.ArgExpression.LongIdentifier?.QualifiedName;
        if (arg != null && arg.Equals("CompilationRepresentationFlags.ModuleSuffix", StringComparison.Ordinal))
          hasModuleSuffix = true;
      }
      var sourceName = identifier?.Name;
      var compiledName = hasModuleSuffix && sourceName != null ? sourceName + "Module" : sourceName;
      return compiledName ?? SharedImplUtil.MISSING_DECLARATION_NAME;
    }

    [NotNull]
    public static string GetSourceName([CanBeNull] IIdentifier identifier)
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
    public static string[] GetQualifiersAndName(FSharpIdentifierToken token)
    {
      var longIdentifier = token.Parent as ILongIdentifier;
      if (longIdentifier == null) return new[] {FSharpNamesUtil.RemoveBackticks(token.GetText())};

      var names = new FrugalLocalHashSet<string>();
      foreach (var id in longIdentifier.IdentifiersEnumerable)
      {
        names.Add(FSharpNamesUtil.RemoveBackticks(id.GetText()));
        if (id == token) break;
      }
      return names.ToArray();
    }

    [NotNull]
    public static string MakeClrName([NotNull] IFSharpTypeElementDeclaration declaration)
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

      var typeParamsOwner = declaration as IFSharpTypeDeclaration;
      if (typeParamsOwner?.TypeParameters.Count > 0)
        clrName.Append("`" + typeParamsOwner.TypeParameters.Count);

      return clrName.ToString();
    }

    [NotNull]
    public static string GetMemberCompiledName([NotNull] this FSharpMemberOrFunctionOrValue mfv)
    {
      var compiledNameAttr = mfv.Attributes.FirstOrDefault(a => a.AttributeType.FullName == CompiledNameAttrName);
      var compiledName = compiledNameAttr != null && !compiledNameAttr.ConstructorArguments.IsEmpty()
        ? compiledNameAttr.ConstructorArguments[0].Item2 as string
        : null;
      return compiledName ??
             (mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod
               ? mfv.DisplayName
               : mfv.LogicalName);
    }

    public static FSharpFileKind GetFSharpFileKind([NotNull] this IFile file)
    {
      if (file is IFSharpImplFile) return FSharpFileKind.ImplFile;
      if (file is IFSharpSigFile) return FSharpFileKind.SigFile;
      throw new ArgumentOutOfRangeException();
    }

    public static FSharpFileKind GetFSharpFileKind([NotNull] this IPsiSourceFile sourceFile)
    {
      var fileExtension = sourceFile.GetLocation().ExtensionNoDot;
      if (fileExtension == "fs" || fileExtension == "ml") return FSharpFileKind.ImplFile;
      if (fileExtension == "fsi" || fileExtension == "mli") return FSharpFileKind.SigFile;
      throw new ArgumentOutOfRangeException();
    }

    [CanBeNull]
    internal static IDeclaredElement GetActivePatternByIndex(IDeclaration declaration, int index)
    {
      var letDecl = declaration as Let;
      var cases = letDecl?.Identifier.Children<ActivePatternCaseDeclaration>().AsIList();
      return cases?.Count > index ? cases[index].DeclaredElement : null;
    }

    [NotNull]
    public static string GetNestedModuleShortName(INestedModuleDeclaration declaration, ICacheBuilder cacheBuilder)
    {
      var parent = declaration.Parent as IModuleLikeDeclaration;
      var shortName = declaration.ShortName;
      var moduleName =
        parent?.Children<IFSharpTypeDeclaration>().Any(t => t.ShortName == shortName) ?? false
          ? shortName + "Module"
          : shortName;
      return cacheBuilder.Intern(moduleName);
    }
  }
}