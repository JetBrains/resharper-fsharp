namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open JetBrains.ProjectModel
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.UI.Icons

[<ProjectModelElementPresenter(2.0)>]
type FSharpProjectPresenter() =
    interface IProjectModelElementPresenter with
        member x.GetIcon(projectModelElement) =
            match projectModelElement with
            | :? IProject as project when (project.ProjectProperties :? FSharpProjectProperties) ->
                ProjectModelThemedIcons.FsharpProject.Id
            | _ -> null

        member x.GetPresentableLocation _ = null