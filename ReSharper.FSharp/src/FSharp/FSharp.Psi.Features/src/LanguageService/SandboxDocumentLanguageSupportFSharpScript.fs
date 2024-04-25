namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open System
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel
open JetBrains.RdBackend.Common.Features.Documents
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.RdBackend.Common.Env

[<Language(typeof<FSharpLanguage>)>]
[<ZoneMarker(typeof<IReSharperHostNetFeatureZone>)>]
type SandboxDocumentLanguageSupportFSharpScript() =
    interface ISandboxDocumentLanguageSupport with
        member x.GetDocumentFileExtension _ = FSharpScriptProjectFileType.FsxExtension
        member x.ProjectFileType = FSharpScriptProjectFileType.Instance :> ProjectFileType
        member x.SetupSandboxFile(_, _, _) = ()
        member x.GetExtraInfos _ = Nullable()
