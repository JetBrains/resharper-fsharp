namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open JetBrains.Application
open JetBrains.Application.BuildScript
open JetBrains.ReSharper.Host.Features.Settings
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Psi

[<ShellComponent>]
type FSharpFileService(settingsLocation: RiderAnyProductSettingsLocation) =
    let scratchesDir =
        // params are arbitrary, they should not be used inside this override
        settingsLocation.GetSettingsPath(HostFolderLifetime.TempFolder, ApplicationHostDetails.PerHost)
            .Parent / "scratches"

    member x.IsScript(file: IPsiSourceFile) =
        file.LanguageType.Equals(FSharpScriptProjectFileType.Instance) ||
        file.GetLocation().Parent.Equals(scratchesDir)
