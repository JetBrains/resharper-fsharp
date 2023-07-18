namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AI

open JetBrains.Application.Parts
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.AI
open JetBrains.ReSharper.Feature.Services.AI.Context.Common
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts

[<SolutionComponent(Instantiation.DemandAnyThread)>]
[<ZoneMarker(typeof<IArtificialIntelligenceZone>)>]
type FSharpLanguageOrTechnologyChatContextProvider() =
    interface ILanguageOrTechnologyChatContextProvider with
        member this.GetLanguageOrTechnologyPresentation(psiModule) =
            if isFSharpProjectModule psiModule || psiModule :? FSharpScriptPsiModule then "F#" else null
