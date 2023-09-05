namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AI

open JetBrains.Application.Parts
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.AI
open JetBrains.ReSharper.Feature.Services.ChatContexts
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts

[<SolutionComponent(Instantiation.DemandAnyThread)>]
type FSharpLanguageOrTechnologyChatContextProvider() =
    let _ = Unchecked.defaultof<IArtificialIntelligenceZone>

    interface ILanguageOrTechnologyChatContextProvider with
        member this.GetLanguageOrTechnologyPresentation(psiModule) =
            if isFSharpProjectModule psiModule || psiModule :? FSharpScriptPsiModule then "F#" else null

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IArtificialIntelligenceZone>