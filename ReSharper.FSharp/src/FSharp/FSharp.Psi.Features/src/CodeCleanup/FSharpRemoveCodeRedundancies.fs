module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCleanup.FSharpRemoveCodeRedundancies

open System
open JetBrains.ReSharper.Feature.Services.CodeCleanup
open JetBrains.ReSharper.Feature.Services.CodeCleanup.HighlightingModule
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings

[<CodeCleanupModule>]
type FSharpRemoveCodeRedundancies() =
    inherit HighlightingCleanupModule()

    let descriptors = FSharpCodeCleanupDescriptors.descriptors

    override this.IsAvailable(profile: CodeCleanupProfile) =
        profile.IsAnySettingsOn(descriptors)

    override this.SetDefaultSetting(profile, profileType) =
        match profileType with
        | CodeCleanupService.DefaultProfileType.FULL ->
            profile.SetAll(descriptors, true)

        | CodeCleanupService.DefaultProfileType.CODE_STYLE
        | CodeCleanupService.DefaultProfileType.REFORMAT ->
            profile.SetAll(descriptors, false)

        | _ -> ArgumentOutOfRangeException() |> raise

    override this.Descriptors = [| for descriptor in descriptors -> descriptor |]
    override this.LanguageType = FSharpLanguage.Instance
    override this.Name = "F# Code Cleanup"
