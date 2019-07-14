namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell

type FSharpElementFactory(languageService: IFSharpLanguageService, psiModule: IPsiModule) =
    let [<Literal>] moniker = "F# element factory"

    let createDocument source =
        let documentFactory = Shell.Instance.GetComponent<IInMemoryDocumentFactory>()
        let document = documentFactory.CreateSimpleDocumentFromText(source, moniker) 
        languageService.CreateParser(document).ParseFile() :?> _

    let getModuleDeclaration source =
        let fsFile = createFile source
        fsFile.Declarations.First()

    interface IFSharpElementFactory with
        member x.CreateOpenStatement(ns) =
            let source = "open " + ns
            let moduleDeclaration = getModuleDeclaration source

            moduleDeclaration.Members.First() :?> _

        member x.CreateWildPat() =
            let source = "let _ = ()"
            let moduleDeclaration = getModuleDeclaration source

            let letModuleDecl = moduleDeclaration.Members.First().As<ILetModuleDecl>()
            let binding = letModuleDecl.Bindings.First()
            binding.HeadPattern :?> _

        member x.CreateIgnoreApp(expr) =
            let source = "() |> ignore"
            let moduleDeclaration = getModuleDeclaration source

            let doDecl = moduleDeclaration.Members.First().As<IDo>().NotNull()

            match doDecl.Expression.As<IAppExpr>() with
            | null -> failwith "1"
            | outerAppExpr ->

            match outerAppExpr.FunctionExpression.As<IAppExpr>() with
            | null -> failwith "2"
            | innerAppExpr ->

            replace innerAppExpr.FunctionExpression expr
            outerAppExpr