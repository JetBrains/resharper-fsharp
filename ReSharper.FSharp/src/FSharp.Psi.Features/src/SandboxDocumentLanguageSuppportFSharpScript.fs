module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Document.SandboxDocumentLanguageSupportFSharpScript

open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features.Documents
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi


[<Language(typeof<FSharpScriptLanguage>)>]
type SandboxDocumentLanguageSupportFSharpScript() =
    interface ISandboxDocumentLanguageSupport with 
        member val DocumentFileExtension = FSharpScriptProjectFileType.FsScriptExtension with get
        member val ProjectFileType = FSharpScriptProjectFileType.Instance :> ProjectFileType  with get
        member x.SetupSandboxFile(sandboxFile, sandboxInfo, lifetime) = ()
        member x.GetExtraInfos(sandboxDocumentInfo) = null