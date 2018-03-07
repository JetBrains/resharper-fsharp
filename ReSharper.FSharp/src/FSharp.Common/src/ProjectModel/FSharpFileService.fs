namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open JetBrains.Application
open JetBrains.Application.BuildScript
open JetBrains.ReSharper.Host.Features.Settings
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Psi

type IFSharpFileService =
    abstract member IsScript: IPsiSourceFile -> bool

[<ShellComponent>]
type FSharpFileService(settingsLocation: RiderAnyProductSettingsLocation) =
    let scratchesDir =
        // params are arbitrary, they should not be used inside this override
        settingsLocation.GetSettingsPath(HostFolderLifetime.TempFolder, ApplicationHostDetails.PerHost)
            .Parent / "scratches"

    interface IFSharpFileService with
        member x.IsScript(file: IPsiSourceFile) =
            file.PsiModule :? FSharpScriptPsiModule ||
            file.GetLocation().Parent.Equals(scratchesDir)
