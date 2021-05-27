using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public class FSharpDeclaredElementType : DeclaredElementTypeBase
  {
    public static readonly DeclaredElementType ActivePatternCase = new FSharpDeclaredElementType("active pattern case", null);

    public FSharpDeclaredElementType(string name, [CanBeNull] IconId imageName) : base(name, imageName)
    {
    }

    protected override IDeclaredElementPresenter DefaultPresenter => CSharpDeclaredElementPresenter.Instance;

    public override bool IsPresentable(PsiLanguageType language)
    {
      return language.Is<FSharpLanguage>();
    }
  }
}