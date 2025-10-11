namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open JetBrains.Application
open JetBrains.Application.BuildScript
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Parts
open JetBrains.ProjectModel
open JetBrains.RdBackend.Common.Env
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Psi
open JetBrains.Rider.Backend.Env

[<ShellComponent(Instantiation.DemandAnyThreadSafe)>]
[<ZoneMarker(typeof<IReSharperHostNetFeatureZone>, typeof<IRiderBackendFeatureZone>, typeof<IRiderFeatureZone>)>]
type FSharpFileService(settingsLocation: RiderAnyProductSettingsLocation, fileExtensions: IProjectFileExtensions) =
    let scratchesDir =
        // Parameters are arbitrary, they aren't currently used inside this override.
        settingsLocation.GetUserSettingsDir(HostFolderLifetime.TempFolder, ApplicationHostDetails.PerHost)
            .Parent / "scratches"

    interface IFSharpFileService with
        member x.IsScratchFile(path: VirtualFileSystemPath) =
            path.Parent.Equals(scratchesDir)

        member x.IsScriptLike(file) =
            fileExtensions.GetFileType(file.GetLocation().ExtensionWithDot).Is<FSharpScriptProjectFileType>() ||
            file.PsiModule :? FSharpScriptPsiModule ||
            file.GetLocation().Parent.Equals(scratchesDir)
