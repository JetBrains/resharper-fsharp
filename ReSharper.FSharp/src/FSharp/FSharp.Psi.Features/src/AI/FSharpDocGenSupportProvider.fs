namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AI

open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.RdBackend.Common.Features.Util.Ranges
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Rider.Backend.Features.AI.DocGen
open JetBrains.Rider.Model

[<Language(typeof<FSharpLanguage>)>]
type FSharpDocGenSupportProvider() =
    interface IAiDocGenSupportProvider with
        member this.IsApplicableForInlay(declaration: IDeclaration) =
            let docGenSupportProvider = this :> IAiDocGenSupportProvider
            if not (docGenSupportProvider.IsApplicable(declaration)) then false else
                let docNode = this.FindDocNode(declaration)
                isNotNull(docNode) && docNode.GetXmlPsi().XmlFile.IsIncompleteDoc()
        
        member this.IsApplicable(declaration: IDeclaration) =
            declaration :? IAttributesOwnerDeclaration &&
            not(declaration :? IPrimaryConstructorDeclaration)
        
        member this.CalculateContext(declaration: IDeclaration) =
            let docNode = this.FindDocNode(declaration)
            let docRange = docNode.GetDocumentRange()
            GenericContextEntry(declaration.DeclaredName, docRange.TextRange.ToRdTextRange())
        
        member this.UpdateDocText(document: IDocument, declaration: IDeclaration, text: string) =
            let solution = declaration.GetSolution()
            let docNode = this.FindDocNode(declaration)
            
            // sometimes there is an empty XmlDocBlock and DocComment right after it
            let actualDocNode =
                if isNotNull(docNode) && docNode.GetDocumentRange().IsEmpty && docNode.NextSibling :? DocComment
                   then docNode.NextSibling else docNode
                   
            let transformedNode = this.TransformNode(declaration)
            let newDocRange = document.InsertOrReplaceCommentText(transformedNode, actualDocNode, text)
            solution.GetComponent<IPsiFiles>().CommitAllDocuments();
            newDocRange
            
        member this.UpdateDocUnderTransaction(declaration: IDeclaration, docRange: DocumentRange, text: string) =
            docRange
        
        member this.FindTarget(node: ITreeNode) =
            match node with
            | :? IDeclaration as decl -> decl
            | _ -> null
        
        member this.GetText(declaration: IDeclaration) =
            declaration.GetText()
            
    member this.FindDocNode(declaration: IDeclaration) =
        match declaration with
        | :? IFSharpDeclaration as fsharpDecl -> fsharpDecl.XmlDocBlock
        | :? ITopBinding as binding ->
            match binding.FirstChild with
            | :? XmlDocBlock as docBlock -> docBlock
            | _ -> null
        | _ -> null
    
    member this.TransformNode(declaration: IDeclaration) =
            match declaration with
            // ITypeExtensionDeclaration range does not include `type` keyword
            | :? ITypeExtensionDeclaration as typeExtension -> typeExtension.Parent
            | _ -> declaration