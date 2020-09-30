namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService

open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features.Documents
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi

[<Language(typeof<FSharpScriptLanguage>)>]
type SandboxDocumentLanguageSupportFSharpScript() =
    interface ISandboxDocumentLanguageSupport with 
        member x.DocumentFileExtension = FSharpScriptProjectFileType.FsxExtension
        member x.ProjectFileType = FSharpScriptProjectFileType.Instance :> ProjectFileType
        member x.SetupSandboxFile(_, _, _) = ()
        member x.GetExtraInfos _ = null
