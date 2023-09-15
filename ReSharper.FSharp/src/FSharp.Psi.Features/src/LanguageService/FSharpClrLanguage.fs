namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open JetBrains.Application
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Feature.Services.ClrLanguages

[<ShellComponent>]
type FSharpClrLanguage() =
    interface IClrLanguagesKnown with
        member x.Language = FSharpLanguage.Instance :> _
