using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public class FSharpDeclaredElementType(string name, [CanBeNull] IconId imageName)
    : DeclaredElementTypeBase(name, imageName)
  {
    public static readonly DeclaredElementType ActivePatternCase = new FSharpDeclaredElementType("active pattern case", null);
    public static readonly DeclaredElementType Exception = new FSharpDeclaredElementType("exception", null);
    public static readonly DeclaredElementType Function = new FSharpDeclaredElementType("function", null);
    public static readonly DeclaredElementType ObjectExpression = new FSharpDeclaredElementType("object expression", null);
    public static readonly DeclaredElementType Module = new FSharpDeclaredElementType("module", null);
    public static readonly DeclaredElementType Record = new FSharpDeclaredElementType("record", null);
    public static readonly DeclaredElementType RecordField = new FSharpDeclaredElementType("field", null);
    public static readonly DeclaredElementType TypeAbbreviation = new FSharpDeclaredElementType("type abbreviation", null);
    public static readonly DeclaredElementType Union = new FSharpDeclaredElementType("union", PsiSymbolsThemedIcons.EnumMember.Id);
    public static readonly DeclaredElementType UnionCase = new FSharpDeclaredElementType("union case", PsiSymbolsThemedIcons.EnumMember.Id);
    public static readonly DeclaredElementType Value = new FSharpDeclaredElementType("value", null);

    protected override IDeclaredElementPresenter DefaultPresenter => CSharpDeclaredElementPresenter.Instance;

    public override bool IsPresentable(PsiLanguageType language)
    {
      return language.Is<FSharpLanguage>();
    }
  }
}
