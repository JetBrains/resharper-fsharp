namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features

open System.Collections.Concurrent
open FSharp.Compiler.SourceCodeServices
open JetBrains.Annotations
open JetBrains.Application.Infra
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Psi.XmlIndex
open JetBrains.UI.RichText
open JetBrains.Util

[<SolutionComponent>]
type FSharpXmlDocService
        (psiServices: IPsiServices, xmlDocThread: XmlIndexThread, psiConfig: IPsiConfiguration, psiModules: IPsiModules,
         assemblyInfoDatabase: AssemblyInfoDatabase) =

    let indexCache = ConcurrentDictionary<string, XmlDocIndex>()

    let getIndex dllFile =
        indexCache.TryGetValue(dllFile)
        |> Option.ofObj
        |> Option.orElseWith (fun _ ->
            match FileSystemPath.TryParse(dllFile) with
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
    member x.GetXmlDoc(fsXmlDoc: FSharpXmlDoc) =
        match fsXmlDoc with
        | FSharpXmlDoc.Text (s, _) ->
            let text = s |> Array.map (fun s -> s.Trim()) |> String.concat "\n"
            RichTextBlock(text)

        | FSharpXmlDoc.XmlDocFileSignature (dllFile, memberName) ->
            getIndex dllFile
            |> Option.map (fun index ->
                let summary = XMLDocUtil.ExtractSummary(index.GetXml(memberName))
                XmlDocRichTextPresenter.Run(summary, false, FSharpLanguage.Instance))
            |> Option.defaultValue null

        | FSharpXmlDoc.None -> null
