using JetBrains.Annotations;
using JetBrains.Application.UI.Icons.ComposedIcons;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Psi.FSharp.Impl
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