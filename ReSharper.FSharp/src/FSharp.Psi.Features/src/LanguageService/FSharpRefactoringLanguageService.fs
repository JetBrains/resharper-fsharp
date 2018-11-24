namespace JetBrains.ReSharper.Plugins.FSharp.Services

open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Feature.Services.Refactorings

[<Language(typeof<FSharpLanguage>)>]
type FSharpRefactoringLanguageService() =
    interface IRefactoringLanguageService with
        member x.IsAvailable() = false
