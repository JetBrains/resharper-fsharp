namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Internal

open System.Diagnostics
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.UI.ActionSystem.ActionsRevised.Menu
open JetBrains.Application.UI.Actions.InternalMenu
open JetBrains.Application.UI.ActionsRevised.Menu
open JetBrains.ProjectModel
open JetBrains.ProjectModel.DataContext
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Psi
open JetBrains.Util

[<Action("FSharp_Internal_DumpFcsProjects", "Dump all FCS projects")>]
type DumpFcsProjectsAction() =
    interface IExecutableAction with
        member this.Update(context, _, _) =
            isNotNull (context.GetData(ProjectModelDataConstants.SOLUTION))

        member this.Execute(context, _) =
            let solution = context.GetData(ProjectModelDataConstants.SOLUTION)
            let fcsProjectProvider = solution.GetPsiServices().GetComponent<IFcsProjectProvider>()

            let tempPath = FileSystemDefinition.CreateTemporaryFile(extensionWithDot = ".txt")
            tempPath.WriteTextStreamDenyWrite(fun writer ->
                let allFcsProjects = fcsProjectProvider.GetAllFcsProjects() |> List.ofSeq
                for fcsProject in allFcsProjects do
                    writer.WriteLine(fcsProject.TestDump(writer)))

            Process.Start(tempPath.FullPath) |> ignore


[<Action("FSharp_Internal_DumpCurrentFcsProject", "Dump current FCS project")>]
type DumpCurrentFcsProjectAction() =
    interface IExecutableAction with
        member this.Update(context, _, _) =
            isNotNull (context.GetData(ProjectModelDataConstants.PROJECT))

        member this.Execute(context, _) =
            let project = context.GetData(ProjectModelDataConstants.PROJECT)
            let solution = project.GetSolution()
            let fcsProjectProvider = solution.GetComponent<IFcsProjectProvider>()

            let tempPath = FileSystemDefinition.CreateTemporaryFile(extensionWithDot = ".txt")
            tempPath.WriteTextStreamDenyWrite(fun writer ->
                for psiModule in solution.GetPsiServices().Modules.GetPsiModules(project) do
                    match fcsProjectProvider.GetFcsProject(psiModule) with
                    | Some fcsProject -> writer.WriteLine(fcsProject.TestDump(writer))
                    | _ -> ())

            Process.Start(tempPath.FullPath) |> ignore


[<ActionGroup(ActionGroupInsertStyles.Submenu ||| ActionGroupInsertStyles.Separated, Text = "F#")>]
type FSharpInternalActionGroup(
        _dumpFcsProjectsAction: DumpFcsProjectsAction,
        _dumpCurrentFcsProjectAction: DumpCurrentFcsProjectAction) =
    interface IAction
    interface IInsertLast<IntoInternalMenu>


[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IInternalVisibilityZone>
    // interface IRequire<IReSpellerZone>
