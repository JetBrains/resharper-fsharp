namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open JetBrains.Application
open JetBrains.Application.Parts
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Feature.Services.ClrLanguages

[<ShellComponent(Instantiation.DemandAnyThreadSafe)>]
type FSharpClrLanguage() =
    interface IClrLanguagesKnown with
        member x.Language = FSharpLanguage.Instance :> _
