namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open JetBrains.Application
open JetBrains.Application.BuildScript
open JetBrains.ReSharper.Host.Env
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Psi

[<ShellComponent>]
type FSharpFileService(settingsLocation: RiderAnyProductSettingsLocation) =
    let scratchesDir =
        // Parameters are arbitrary, they aren't currently used inside this override.
        settingsLocation.GetSettingsPath(HostFolderLifetime.TempFolder, ApplicationHostDetails.PerHost)
            .Parent / "scratches"

    interface IFSharpFileService with
        member x.IsScratchFile(path: FileSystemPath) =
            path.Parent.Equals(scratchesDir)

        member x.IsScriptLike(file) =
            file.LanguageType.Equals(FSharpScriptProjectFileType.Instance) ||
            file.PsiModule :? FSharpScriptPsiModule ||
            file.GetLocation().Parent.Equals(scratchesDir)
