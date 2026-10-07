namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService

open JetBrains.ReSharper.Feature.Services.Clipboard
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi

[<Language(typeof<FSharpLanguage>)>]
type FSharpLanguageSpecificCopyPasteBehavior() =
    interface ILanguageSpecificCopyPasteBehavior with
        member x.AllowSmartCopyPaste _ = false
