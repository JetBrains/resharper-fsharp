namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AICore

open System.Text
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Rider.Backend.Features.AICore.Summarization
open JetBrains.ReSharper.Psi.Files

[<Language(typeof<FSharpLanguage>)>]
type FSharpFileSummarizer() =
    let formatRange (range: JetBrains.DocumentModel.DocumentRange) =
        FileSummarizer.PresentInJunieFlavor(range)

    let formatBeginLine (range: JetBrains.DocumentModel.DocumentRange) =
        FileSummarizer.PresentBeginLineInJunieFlavor(range)

    let summarizeStructure (fsharpFile: IFSharpFile): string =
        let sb = StringBuilder()

        let rec summarizeType (_parent: string option) (typeDecl: ITypeDeclaration) =
            // For F#, keep type names unqualified (no parent prefix)
            let typeName = typeDecl.DeclaredName
            let isModule = match box typeDecl with :? IModuleDeclaration -> true | _ -> false
            let headerKeyword = if isModule then "module" else "type"
            let header = headerKeyword + " " + typeName + " (" + (formatRange (typeDecl.GetDocumentRange())) + ")"
            sb.AppendLine(header) |> ignore

            // Collect member declarations if available
            match typeDecl :> obj with
            | :? IFSharpTypeElementDeclaration as fsharpTypeDecl ->
                for m in fsharpTypeDecl.MemberDeclarations do
                    match m with
                    | :? ITypeDeclaration as nestedType ->
                        // For F#, do not qualify nested types with parent name
                        summarizeType None nestedType
                    | :? ITypeMemberDeclaration as memberDecl ->
                        let name =
                            match memberDecl :> obj with
                            | :? IFSharpDeclaration as d when not (isNull (box d.SourceName)) -> d.SourceName
                            | _ -> "member"
                        let line =
                            if isModule then
                                // Top-level functions/values in module
                                "val " + name + " (" + (formatRange (memberDecl.GetDocumentRange())) + ")"
                            else
                                typeName + " member " + name + " (" + (formatRange (memberDecl.GetDocumentRange())) + ")"
                        sb.AppendLine(line) |> ignore
                    | _ -> ()
            | _ -> ()

        // top-level modules
        for m in fsharpFile.ModuleDeclarationsEnumerable do
            match m :> obj with
            | :? ITypeDeclaration as typeDecl -> summarizeType None typeDecl
            | _ -> ()

        sb.ToString()

    let getImportsFromFSharpFile (fsharpFile: IFSharpFile): string =
        let namesWithLines = System.Collections.Generic.List<string>()
        let rec collectFromModule (moduleDecl: IModuleLikeDeclaration) =
            for memberDecl in moduleDecl.MembersEnumerable do
                match memberDecl with
                | :? IOpenStatement as openStmt ->
                    let refName = openStmt.ReferenceName
                    if not (isNull refName) then
                        let ln = formatBeginLine (openStmt.GetDocumentRange())
                        namesWithLines.Add(ln + ":#" + refName.QualifiedName)
                | :? IModuleLikeDeclaration as nestedModule ->
                    collectFromModule nestedModule
                | _ -> ()
        for moduleDecl in fsharpFile.ModuleDeclarationsEnumerable do
            collectFromModule moduleDecl
        if namesWithLines.Count = 0 then "" else
        namesWithLines
        |> String.concat "\n"

    let processFile(file: IPsiSourceFile, language: FSharpLanguage): string =
        let psiFile = file.GetPrimaryPsiFile()
        match psiFile with
        | :? IFSharpFile as fsharpFile -> summarizeStructure fsharpFile
        | _ -> ""

    let processImports(file: IPsiSourceFile, language: FSharpLanguage): string =
        let psiFile = file.GetPrimaryPsiFile()
        match psiFile with
        | :? IFSharpFile as fsharpFile -> getImportsFromFSharpFile fsharpFile
        | _ -> ""

    interface IRiderFileSummarizer with
        member this.GetSummary(file: IPsiSourceFile, flavor: SummarizationFlavor) =
            file.GetPsiServices().Files.AssertAllDocumentAreCommitted()
            let fsharpLang = FSharpLanguage.Instance
            match fsharpLang with
            | null -> ""
            | language ->
                let imports = processImports(file, language)
                let structure = processFile(file, language)
                let sb = StringBuilder()
                sb.AppendLine("##### Imports") |> ignore
                if imports = "" then sb.AppendLine("(none)") |> ignore else sb.AppendLine(imports) |> ignore
                sb.AppendLine() |> ignore
                sb.AppendLine("##### Structure") |> ignore
                sb.Append(structure) |> ignore
                sb.ToString()
