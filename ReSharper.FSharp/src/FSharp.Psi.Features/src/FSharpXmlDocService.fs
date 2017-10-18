namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features

open System.Collections.Concurrent
open JetBrains.Annotations
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.XmlIndex
open JetBrains.ReSharper.Psi.Util
open JetBrains.UI.RichText
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

[<SolutionComponent>]
type FSharpXmlDocService(xmlDocThread: XmlIndexThread, psiConfig: IPsiConfiguration) =
    let indexForPaths = ConcurrentDictionary<FileSystemPath, XmlDocIndex>()

    [<CanBeNull>]
    member x.GetXmlDoc(fsXmlDoc: FSharpXmlDoc) =
        match fsXmlDoc with
        | FSharpXmlDoc.Text s -> RichTextBlock(s)
        | FSharpXmlDoc.XmlDocFileSignature (dllFile, memberName) ->
            match FileSystemPath.TryParse(dllFile) with
            | dllPath when not (dllPath.IsNullOrEmpty()) ->
                let xmlPath = dllPath.ChangeExtension(ExtensionConstants.Xml)
                let index =
                    match indexForPaths.TryGetValue(dllPath) with
                    | null ->
                        let index = XmlDocIndex(xmlPath, true, psiConfig, xmlDocThread)
                        indexForPaths.[xmlPath] <- index
                        index
                    | index -> index
                let summary = XMLDocUtil.ExtractSummary(index.GetXml(memberName))
                XmlDocRichTextPresenter.Run(summary, false, FSharpLanguage.Instance)
            | _ -> null
        | FSharpXmlDoc.None -> null
