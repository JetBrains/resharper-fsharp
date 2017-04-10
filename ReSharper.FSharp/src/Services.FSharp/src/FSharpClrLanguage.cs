using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.ClrLanguages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.FSharp;

namespace JetBrains.ReSharper.Feature.Services.FSharp
{
  [ShellComponent]
  public class FSharpClrLanguage : IClrLanguagesKnown
  {
    public PsiLanguageType Language => FSharpLanguage.Instance;
  }
}