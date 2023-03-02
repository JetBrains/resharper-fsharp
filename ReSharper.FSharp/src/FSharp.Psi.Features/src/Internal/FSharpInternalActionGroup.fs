namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Internal

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Diagnostics
open JetBrains.Application.UI.ActionSystem.ActionsRevised.Menu
open JetBrains.Application.UI.Actions.InternalMenu
open JetBrains.Application.UI.ActionsRevised.Menu
open JetBrains.ProjectModel
open JetBrains.ProjectModel.DataContext
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Tree

[<Action("FSharp_Internal_DumpFcsProjects", "Dump all FCS projects")>]
type DumpFcsProjectsAction() =
    interface IExecutableAction with
        member this.Update(context, _, _) =
            isNotNull (context.GetData(ProjectModelDataConstants.SOLUTION))

        member this.Execute(context, _) =
            let solution = context.GetData(ProjectModelDataConstants.SOLUTION)
            let fcsProjectProvider = solution.GetPsiServices().GetComponent<IFcsProjectProvider>()

            Dumper.DumpToNotepad(fun writer ->
                let allFcsProjects = fcsProjectProvider.GetAllFcsProjects() |> List.ofSeq
                for fcsProject in allFcsProjects do
                    writer.WriteLine(fcsProject.TestDump(writer)))


[<Action("FSharp_Internal_DumpCurrentFcsProject", "Dump current FCS project")>]
type DumpCurrentFcsProjectAction() =
    interface IExecutableAction with
        member this.Update(context, _, _) =
            isNotNull (context.GetData(ProjectModelDataConstants.PROJECT))

        member this.Execute(context, _) =
            let project = context.GetData(ProjectModelDataConstants.PROJECT)
            let solution = project.GetSolution()
            let fcsProjectProvider = solution.GetComponent<IFcsProjectProvider>()

            Dumper.DumpToNotepad(fun writer ->
                for psiModule in solution.GetPsiServices().Modules.GetPsiModules(project) do
                    match fcsProjectProvider.GetFcsProject(psiModule) with
                    | Some fcsProject -> writer.WriteLine(fcsProject.TestDump(writer))
                    | _ -> ())
            
            
[<Action("FSharp_Internal_DumpCurrentFcsFile", "Dump current FCS file")>]
type DumpCurrentFcsFileAction() =

    [<DefaultValue>]
    val mutable myTextControl: JetBrains.TextControl.ITextControl

    interface IExecutableAction with
        member this.Update(context, _, _) =
            isNotNull (context.GetData(ProjectModelDataConstants.PROJECT))

        member this.Execute(context, _) =
            let editorContext = context.GetData(JetBrains.DocumentModel.DataContext.DocumentModelDataConstants.EDITOR_CONTEXT)
            if isNull editorContext then
                ()
            else
                let sourceFile = context.GetData(JetBrains.ReSharper.Psi.DataContext.PsiDataConstants.SOURCE_FILE)
                let file = if isNull sourceFile then null else sourceFile.GetPsiFile(editorContext.CaretOffset)
                if not (isNull file) then
                    Dumper.DumpToNotepad(fun writer ->
                            let treeNode = file :> ITreeNode
                            writer.WriteLine("Language: {0}", file.Language :> obj)
                            DebugUtil.DumpPsi(writer, treeNode))


[<ActionGroup(ActionGroupInsertStyles.Submenu ||| ActionGroupInsertStyles.Separated, Text = "F#")>]
type FSharpInternalActionGroup(
        _dumpFcsProjectsAction: DumpFcsProjectsAction,
        _dumpCurrentFcsProjectAction: DumpCurrentFcsProjectAction,
        _dumpCurrentFcsFileAction: DumpCurrentFcsFileAction) =
    interface IAction
    interface IInsertLast<IntoInternalMenu>


[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IInternalVisibilityZone>
