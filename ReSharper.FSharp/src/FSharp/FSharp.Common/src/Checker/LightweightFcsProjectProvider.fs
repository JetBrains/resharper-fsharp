namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open FSharp.Compiler.CodeAnalysis
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Parts
open JetBrains.DataFlow
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.RdBackend.Common.Env
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules

[<SolutionComponent(InstantiationEx.LegacyDefault)>]
[<ZoneMarker(typeof<IReSharperHostNetLightweightFeatureZone>)>]
type LightweightFcsProjectProvider(lifetime: Lifetime, checkerService: FcsCheckerService) as this =
    do
        checkerService.FcsProjectProvider <- this
        lifetime.OnTermination(fun _ -> checkerService.FcsProjectProvider <- Unchecked.defaultof<_>) |> ignore

    let projectRemoved = new Signal<FcsProjectKey * FcsProject>("LightweightFcsProjectProvider.ProjectRemoved")

    interface IFcsProjectProvider with
        member this.GetParsingOptions(sourceFile) =
            if isNull sourceFile then sandboxParsingOptions else

            let isScript = sourceFile.LanguageType.Is<FSharpScriptProjectFileType>()
            let defines =
                sourceFile.PsiModule.GetAllDefines()
                |> Seq.map _.Name
                |> Seq.toList

            { FSharpParsingOptions.Default with
                SourceFiles = [| sourceFile.GetLocation().FullPath |]
                ConditionalDefines = ImplicitDefines.getImplicitDefines isScript @ defines
                IsInteractive = isScript
                IsExe = isScript }

        member this.GetFcsProject _ = None
        member this.GetPsiModule _ = None
        member this.IsProjectOutput _ = false
        member this.GetProjectOptions(_: IPsiSourceFile): FSharpProjectOptions option = None
        member this.GetProjectOptions(_: IPsiModule): FSharpProjectOptions option = None
        member this.GetFileIndex _ = 0
        member this.HasPairFile _ = false
        member this.InvalidateReferencesToProject _ = false
        member this.ProjectRemoved = projectRemoved :> _
        member this.PrepareAssemblyShim _ = ()
        member this.GetReferencedModule _ = None
        member this.GetAllReferencedModules() = Seq.empty
        member this.HasFcsProjects = false
        member this.GetAllFcsProjects() = Seq.empty
