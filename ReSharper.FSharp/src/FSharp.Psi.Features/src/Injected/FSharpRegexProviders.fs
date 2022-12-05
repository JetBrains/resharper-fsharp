namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected

open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Features.RegExp.Intellisense
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Caches
open JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi
open JetBrains.ReSharper.Psi.RegExp.ClrRegex
open JetBrains.ReSharper.Psi.RegExp.ClrRegex.Tree
open JetBrains.ReSharper.Psi.RegExp.Resources
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.StringLiteralsUtil

[<SolutionComponent>]
type FSharpRegexInjectionProvider
        (lifetime: Lifetime, solution: ISolution, persistentIndexManager: IPersistentIndexManager,
         providersViewer: InjectionNodeProvidersViewer, injectionTargetLanguage: FSharpLiteralInjectionTarget) =
    inherit LanguageInjectorProviderInLiteralsWithRangeMarkersBase<IClrRegularExpressionFile, FSharpToken, FSharpLiteralInjectionTarget>
            (lifetime, solution, persistentIndexManager, providersViewer, injectionTargetLanguage)

    override _.Icon = PsiRegExpThemedIcons.RegExp.Id
    override _.ProvidedInjectionID = InjectedLanguageIDs.ClrRegExpLanguage
    override _.GetCommentInjectionIDs() = [|InjectedLanguageIDs.ClrRegExpLanguage; "REGEX"|]
    override _.SupportedOriginalLanguage = FSharpLanguage.Instance
    override _.ProvidedLanguage = ClrRegexLanguage.Instance
    override _.SupportsInjectionComment = true


[<SolutionComponent>]
type RegExprPsiProvider(injectorProvider: FSharpRegexInjectionProvider) =
    inherit LiteralsInjectionPsiProvider<FSharpLanguage, ClrRegexLanguage>(injectorProvider, ClrRegexLanguage.Instance)

    override _.ProvidedLanguageCanHaveNestedInjects = false


[<Language(typeof<FSharpLanguage>)>]
type FSharpRegularExpressionCompletionProvider() =
    interface IRegexLanguageSpecificCompletionProvider with
        override this.InitializeContext(owner, _) = owner :? ILiteralExpression
        override this.GetReplacementText(owner, text) =
            match owner.As<ILiteralExpression>() with
            | null -> ""
            | literalExpr ->

            let literalType = literalExpr.Literal.GetTokenType()
            let mutable result = text

            if isRegularStringToken literalType then
                result <- result.Replace(@"\", @"\\");

            if FSharpTokenType.InterpolatedStrings[literalType] then
                result <- result.Replace(@"{", @"{{").Replace(@"}", @"}}")

            result
