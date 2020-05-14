using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi;
using JetBrains.ReSharper.Psi.RegExp.ClrRegex;
using JetBrains.ReSharper.Psi.RegExp.ClrRegex.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Services.Cs.Injected
{
  [SolutionComponent]
  public class FSharpInjectionProvider : LanguageInjectorProviderInLiteralsWithRangeMarkersBase<IClrRegularExpressionFile,
    FSharpToken, FSharpLiteralInjectionTarget>
  {
    public FSharpInjectionProvider(Lifetime lifetime, ISolution solution, IPersistentIndexManager persistentIndexManager,
      InjectionNodeProvidersViewer providersViewer, FSharpLiteralInjectionTarget injectionTargetLanguage) : base(
      lifetime, solution, persistentIndexManager, providersViewer, injectionTargetLanguage)
    {
    }

    public override string ProvidedInjectionID => "FsRegex";
    public override PsiLanguageType SupportedOriginalLanguage => FSharpLanguage.Instance;
    public override PsiLanguageType ProvidedLanguage => ClrRegexLanguage.Instance;
  }
}
