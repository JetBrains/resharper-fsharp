namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AI

open JetBrains.Application.Parts
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.ChatContexts
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts

[<SolutionComponent(Instantiation.DemandAnyThreadSafe)>]
type FSharpLanguageOrTechnologyChatContextProvider() =
    interface ILanguageOrTechnologyChatContextProvider with
        member this.GetLanguageOrTechnologyPresentation(psiModule) =
            if isFSharpProjectModule psiModule || psiModule :? FSharpScriptPsiModule then "F#" else null
