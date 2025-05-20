using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeReferenceName
  {
    public override IFSharpIdentifier FSharpIdentifier => Identifier;
    public string ShortName => FSharpIdentifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;
    public string QualifiedName => this.GetQualifiedName();
    public IList<string> Names => this.GetNames();

    public bool IsQualified => Qualifier != null;
    public FSharpSymbolReference QualifierReference => Qualifier?.Reference;

    public void SetQualifier(IClrDeclaredElement declaredElement)
    {
      if (Qualifier != null) return;

      this.SetQualifier(this.CreateElementFactory().CreateTypeReferenceName, declaredElement);
    }
  }

  internal partial class ExpressionReferenceName
  {
    public override IFSharpIdentifier FSharpIdentifier => Identifier;

    public override FSharpReferenceContext? ReferenceContext =>
      ReferencePatNavigator.GetByReferenceName(this) != null
        ? FSharpReferenceContext.Pattern
        : FSharpReferenceContext.Expression;

    protected override FSharpSymbolReference CreateReference() => new(this);

    public string ShortName => FSharpIdentifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;
    public string QualifiedName => this.GetQualifiedName();
    public IList<string> Names => this.GetNames();

    public bool IsQualified => Qualifier != null;
    public FSharpSymbolReference QualifierReference => Qualifier?.Reference;

    public void SetQualifier(IClrDeclaredElement declaredElement)
    {
      if (Qualifier != null) return;

      this.SetQualifier(this.CreateElementFactory().CreateExpressionReferenceName, declaredElement);
    }
  }

  public static class ReferenceNameExtensions
  {
    [NotNull]
    public static string GetQualifiedName([NotNull] this IReferenceName referenceName)
    {
      var qualifier = referenceName.Qualifier;
      var shortName = referenceName.ShortName;

      return qualifier == null
        ? shortName
        : qualifier.QualifiedName + "." + shortName;
    }

    [CanBeNull]
    public static IReferenceName GetFirstQualifier([NotNull] this IReferenceName referenceName)
    {
      var qualifier = referenceName.Qualifier;
      while (qualifier != null)
      {
        referenceName = qualifier;
        qualifier = referenceName.Qualifier;
      }

      return referenceName;
    }

    public static IReferenceName GetFirstName([NotNull] this IReferenceName referenceName) =>
      referenceName.GetFirstQualifier() ?? referenceName;

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
  }
}
