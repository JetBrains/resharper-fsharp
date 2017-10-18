namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features

open JetBrains.Annotations
open JetBrains.Application.platforms
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Impl.reflection2.AssemblyFileLoaderZoned
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.XmlIndex
open JetBrains.ReSharper.Psi.Util
open JetBrains.UI.RichText
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices
open JetBrains.ProjectModel.Assemblies.Impl
open JetBrains.Metadata.Reader.API

[<SolutionComponent>]
type FSharpXmlDocService(xmlDocThread: XmlIndexThread, psiConfig: IPsiConfiguration) =
    [<CanBeNull>]
    member x.GetXmlDoc(fsXmlDoc: FSharpXmlDoc) =
        match fsXmlDoc with
        | FSharpXmlDoc.Text s -> RichTextBlock(s)
        | FSharpXmlDoc.XmlDocFileSignature (dllFile, memberName) ->
            match FileSystemPath.TryParse(dllFile) with
            | dllPath when not (dllPath.IsNullOrEmpty()) ->
                let xmlFile = dllPath.ChangeExtension(ExtensionConstants.Xml)
                let index = XmlDocIndex(xmlFile, true, psiConfig, xmlDocThread)
                let summary = XMLDocUtil.ExtractSummary(index.GetXml(memberName))
                XmlDocRichTextPresenter.Run(summary, false, FSharpLanguage.Instance)
            | _ -> null
        | FSharpXmlDoc.None -> null
