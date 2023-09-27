namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open JetBrains.ProjectModel
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts

[<ProjectModelElementPresenter(2.0)>]
type FSharpProjectPresenter() =
    interface IProjectModelElementPresenter with
        member x.GetIcon(projectModelElement) =
            match projectModelElement with
            | :? IProject as project when (project.ProjectProperties :? FSharpProjectProperties) ->
                ProjectModelThemedIcons.FsharpProject.Id
            | :? FSharpScriptModule ->
                ProjectModelThemedIcons.Fsharp.Id
            | _ -> null

        member x.GetPresentableLocation _ = null