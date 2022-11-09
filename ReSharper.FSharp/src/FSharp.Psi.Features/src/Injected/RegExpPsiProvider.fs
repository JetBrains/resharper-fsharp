namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected

open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected
open JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi
open JetBrains.ReSharper.Psi.RegExp.ClrRegex

[<SolutionComponent>]
type RegExprPsiProvider(injectorProvider: FSharpRegexInjectionProvider) =
    inherit LiteralsInjectionPsiProvider<FSharpLanguage, ClrRegexLanguage>(injectorProvider, ClrRegexLanguage.Instance)

    override _.ProvidedLanguageCanHaveNestedInjects = false
