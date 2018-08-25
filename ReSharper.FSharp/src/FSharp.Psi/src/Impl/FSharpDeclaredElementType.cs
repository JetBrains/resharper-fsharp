using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public class FSharpDeclaredElementType : DeclaredElementTypeBase
  {
    public static readonly DeclaredElementType ActivePatternCase = new FSharpDeclaredElementType("active pattern case", null);

    // these two distinct types were added for presenting elements icons in other languages
    // todo: use single element type and provide additional union type members like implicit accessors for properties
    public static readonly DeclaredElementType UnionCaseProperty = new FSharpDeclaredElementType("union case", PsiSymbolsThemedIcons.Property.Id);
    public static readonly DeclaredElementType UnionCaseClass = new FSharpDeclaredElementType("union case", PsiSymbolsThemedIcons.Class.Id);

    public FSharpDeclaredElementType(string name, [CanBeNull] IconId imageName) : base(name, imageName)
    {
    }

    protected override IDeclaredElementPresenter DefaultPresenter => CSharpDeclaredElementPresenter.Instance;

    public override bool IsPresentable(PsiLanguageType language)
    {
      return language.Is<FSharpLanguage>();
    }
  }

  public static class DeclaredElementTypeEx
  {
    public static bool IsUnionCase(this DeclaredElementType elementType) =>
      elementType == FSharpDeclaredElementType.UnionCaseProperty ||
      elementType == FSharpDeclaredElementType.UnionCaseClass;

    public static bool IsEntity(this DeclaredElementType elementType) =>
      elementType == CLRDeclaredElementType.ENUM ||
      elementType == CLRDeclaredElementType.CLASS ||
      elementType == CLRDeclaredElementType.STRUCT ||
      elementType == CLRDeclaredElementType.DELEGATE ||
      elementType == CLRDeclaredElementType.INTERFACE ||
      elementType == CLRDeclaredElementType.NAMESPACE;
  }
}
