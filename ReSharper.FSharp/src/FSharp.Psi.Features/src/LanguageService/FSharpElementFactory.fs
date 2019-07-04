namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell

type IFSharpElementFactory =
    abstract CreateOpenStatement: ns: string -> IOpenStatement

type FSharpElementFactory(languageService: FSharpLanguageService, psiModule: IPsiModule) =
    let [<Literal>] moniker = "F# element factory"

    let createFile source: IFSharpFile =
        let documentFactory = Shell.Instance.GetComponent<IInMemoryDocumentFactory>()
        let document = documentFactory.CreateSimpleDocumentFromText(source, moniker) 

        let parser = languageService.CreateParser(document, psiModule)
        parser.ParseFile() :?> _

    interface IFSharpElementFactory with
        member x.CreateOpenStatement(ns) =
            let source = "open " + ns
            let fsFile = createFile source

            fsFile.Declarations.First().Members.First() :?> _
