module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCleanup.FSharpRemoveCodeRedundancies

open JetBrains.Application.Parts
open JetBrains.ReSharper.Feature.Services.CodeCleanup
open JetBrains.ReSharper.Feature.Services.CodeCleanup.HighlightingModule
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.FSharpCodeCleanupDescriptors

[<CodeCleanupModule>] //[CodeCleanupModule(ModulesAfter = [typeof(OptimizeUsings)])]
type FSharpRemoveCodeRedundancies() =
    inherit HighlightingCleanupModule()

    let descriptors = descriptors

    override this.IsAvailable(profile: CodeCleanupProfile) =
        descriptors |> Seq.exists profile.GetSetting

    override this.SetDefaultSetting(profile, profileType) =
        match profileType with
        | CodeCleanupService.DefaultProfileType.FULL ->
            for descriptor in descriptors do
                profile.SetSetting<bool>(descriptor, true)

        | _ ->
            for descriptor in descriptors do
                profile.SetSetting<bool>(descriptor, false)

    override this.Descriptors = [|for descriptor in descriptors -> descriptor|]
    override this.IsAvailableOnSelection = true
    override this.LanguageType = FSharpLanguage.Instance
    override this.Name = "F# Code Cleanup"
