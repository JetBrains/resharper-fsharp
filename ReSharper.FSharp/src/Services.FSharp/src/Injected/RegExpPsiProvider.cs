using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi;
using JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi;
using JetBrains.ReSharper.Psi.RegExp.ClrRegex;

namespace JetBrains.ReSharper.Plugins.FSharp.Services.Cs.Injected
{
  [SolutionComponent]
  public class RegExpPsiProvider : LiteralsInjectionPsiProvider<FSharpLanguage, ClrRegexLanguage>
  {
    public RegExpPsiProvider(FSharpInjectionProvider injectorProvider)
      : base(injectorProvider, ClrRegexLanguage.Instance)
    {
    }

    public override bool ProvidedLanguageCanHaveNestedInjects => false;
  }
}
