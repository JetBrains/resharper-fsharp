using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl
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

    [NotNull]
    public static string GetName([CanBeNull] IFSharpIdentifier identifier,
      TreeNodeCollection<IFSharpAttribute> attributes)
    {
      var hasModuleSuffix = false;

      foreach (var attr in attributes)
      {
        if (attr.LongIdentifier?.Name.SubstringBeforeLast("Attribute") == "CompiledName" &&
            attr.ArgExpression.String != null) // todo: proper expressions evaluation, e.g. "S1" + "S2"
        {
          var compiledNameString = attr.ArgExpression.String.GetText();
          return compiledNameString.Substring(1, compiledNameString.Length - 2);
        }

        if (!hasModuleSuffix &&
            attr.LongIdentifier?.Name.SubstringBeforeLast("Attribute") == "CompilationRepresentation" &&
            attr.ArgExpression.LongIdentifier?.QualifiedName == "CompilationRepresentationFlags.ModuleSuffix")
          hasModuleSuffix = true;
      }
      var sourceName = identifier?.Name;
      var compiledName = hasModuleSuffix && sourceName != null ? sourceName + "Module" : sourceName;
      return compiledName ?? SharedImplUtil.MISSING_DECLARATION_NAME;
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

      var typeParamsOwner = declaration as IFSharpTypeParametersOwnerDeclaration;
      if (typeParamsOwner?.TypeParameters.Count > 0)
        clrName.Append("`" + typeParamsOwner.TypeParameters.Count);

      return clrName.ToString();
    }

    [NotNull]
    public static string GetMemberCompiledName([NotNull] FSharpMemberOrFunctionOrValue mfv)
    {
      var compiledNameAttr = mfv.Attributes.FirstOrDefault(a => a.AttributeType.FullName == CompiledNameAttrName);
      var compiledName = compiledNameAttr != null && !compiledNameAttr.ConstructorArguments.IsEmpty()
        ? compiledNameAttr.ConstructorArguments[0].Item2 as string
        : null;
      return compiledName ?? mfv.LogicalName;
    }
  }
}