namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AI

open JetBrains.Application.Parts
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.ChatContexts.Common
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts

[<SolutionComponent(Instantiation.DemandAnyThreadSafe)>]
type FSharpModuleLanguageDetailsProvider() =
    interface IModuleLanguageDetailsProvider with
        member this.GetLanguageDetails(psiModule) =
            if isFSharpProjectModule psiModule || psiModule :? FSharpScriptPsiModule then
                LanguageDetails("F#")
            else
                null
