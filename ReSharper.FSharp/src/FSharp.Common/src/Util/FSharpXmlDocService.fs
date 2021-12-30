namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features

open System.Collections.Concurrent
open System.Xml
open FSharp.Compiler.Symbols
open JetBrains.Annotations
open JetBrains.Application.Infra
open JetBrains.Application.UI.Components.Theming
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.QuickDoc.Render
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Psi.XmlIndex
open JetBrains.UI.RichText
open JetBrains.Util

[<SolutionComponent>]
type FSharpXmlDocService(psiServices: IPsiServices, xmlDocThread: XmlIndexThread, psiConfig: IPsiConfiguration,
        psiModules: IPsiModules, assemblyInfoDatabase: AssemblyInfoDatabase, theming: ITheming, factory: XmlDocSectionFactory) =

    static let сrefManager =
        { new CrefManager() with
            member x.Process(cref, _, _, _, _) = XmlDocPresenterUtil.ProcessCref(cref)
            member x.Create _ = null }

    let indexCache = ConcurrentDictionary<string, XmlDocIndex>()

    let formatXmlDoc (node: XmlNode) =
        let result = RichText()
        XmlDocHtmlPresenter
            .ConvertProcessor(node, null, null :> DeclaredElementInstance, false, FSharpLanguage.Instance, сrefManager, factory, theming)
            .AppendTextBody(result, true)
        result

    let getIndex dllFile =
        indexCache.TryGetValue(dllFile)
        |> Option.ofObj
        |> Option.orElseWith (fun _ ->
            match VirtualFileSystemPath.TryParse(dllFile, InteractionContext.SolutionContext) with
            | dllPath when not (dllPath.IsNullOrEmpty()) ->
                let assemblyName = assemblyInfoDatabase.GetAssemblyName(dllPath)
                let index =
                    assemblyName
                    |> Option.ofObj
                    |> Option.bind (fun assemblyName ->
                        let psiModules = psiModules.GetAssemblyPsiModuleByName(assemblyName)
                        psiModules |> Seq.tryFind (fun psiModule -> psiModule.Assembly.Location.Equals(dllPath)))
                    |> Option.map (fun psiModule ->
                        let assemblyFile = psiServices.Symbols.GetLibraryFile(psiModule.Assembly)
                        assemblyFile.XmlDocIndex)
                    |> Option.defaultWith (fun _ ->
                        XmlDocIndex(dllPath.ChangeExtension(ExtensionConstants.Xml), true, psiConfig, xmlDocThread))
                indexCache.[dllFile] <- index
                Some index
            | _ -> None)

    [<CanBeNull>]
    member x.GetXmlDoc(fsXmlDoc: FSharpXmlDoc, summaryOnly) =
        let xmlNode =
            match fsXmlDoc with
            | FSharpXmlDoc.FromXmlText(xmlDoc) ->
                let xmlDocument = XmlDocument()
                try
                    xmlDocument.LoadXml("<root>" + xmlDoc.GetXmlText() + "</root>")
                    xmlDocument.SelectSingleNode("root")
                with e ->
                    xmlDocument.LoadXml("<summary>" + e.Message + "</summary>")
                    xmlDocument :> _

            | FSharpXmlDoc.FromXmlFile (dllFile, memberName) ->
                getIndex dllFile
                |> Option.map (fun index -> index.GetXml(memberName) |> fst)
                |> Option.defaultValue null

            | FSharpXmlDoc.None -> null

        if isNull xmlNode || xmlNode.InnerText.IsNullOrWhitespace() then null else

        if summaryOnly then
            let summary = XMLDocUtil.ExtractSummary(xmlNode)
            XmlDocRichTextPresenter.Run(summary, false, FSharpLanguage.Instance)
        else
            xmlNode |> formatXmlDoc |> RichTextBlock
