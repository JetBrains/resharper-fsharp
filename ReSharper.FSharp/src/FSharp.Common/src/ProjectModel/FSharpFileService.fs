namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open JetBrains.Application
open JetBrains.Application.BuildScript
open JetBrains.ReSharper.Host.Features.Settings
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Psi

type IFSharpFileService =
    /// True when file is script or an IntelliJ scratch file.
    abstract member IsScriptLike: IPsiSourceFile -> bool

    /// True when file is an IntelliJ scratch file.
    abstract member IsScratchFile: IPsiSourceFile -> bool

[<ShellComponent>]
type FSharpFileService(settingsLocation: RiderAnyProductSettingsLocation) =
    let scratchesDir =
        // Parameters are arbitrary, they aren't currently used inside this override.
        settingsLocation.GetSettingsPath(HostFolderLifetime.TempFolder, ApplicationHostDetails.PerHost)
            .Parent / "scratches"

    interface IFSharpFileService with
        member x.IsScratchFile(file) =
            file.GetLocation().Parent.Equals(scratchesDir)

        member x.IsScriptLike(file) =
            file.LanguageType.Equals(FSharpScriptProjectFileType.Instance) ||
            file.PsiModule :? FSharpScriptPsiModule ||
            file.GetLocation().Parent.Equals(scratchesDir)
