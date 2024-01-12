namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AI

open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.RdBackend.Common.Features.Util.Ranges
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
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
            let declaredName =
                if declaration.DeclaredName == SharedImplUtil.MISSING_DECLARATION_NAME && isNotNull(declaration.DeclaredElement)
                    then declaration.DeclaredElement.ShortName else declaration.DeclaredName
            GenericContextEntry(declaredName, docRange.TextRange.ToRdTextRange())
        
        member this.UpdateDocText(document: IDocument, declaration: IDeclaration, text: string) =
            let solution = declaration.GetSolution()
            let docNode = this.FindDocNode(declaration)
            
            // sometimes there is an empty XmlDocBlock and DocComment right after it
            //
            // Example:
            // type MyType() =
            //     member _.t = 2
            //
            //     /// 
            //     member _.TestProperty
            //         with get() = t
            //
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
            node.As<IDeclaration>()
        
        member this.GetText(declaration: IDeclaration) =
            Seq.singleton(declaration.GetText())
        
        member this.FindDocExample(declaration: IDeclaration) = null
            
    member this.FindDocNode(declaration: IDeclaration) =
        match declaration with
        | :? IFSharpDeclaration as fsharpDecl -> fsharpDecl.XmlDocBlock
        | :? ITopBinding as binding -> binding.FirstChild.As<XmlDocBlock>()
        | _ -> null
    
    // ITypeExtensionDeclaration range does not include `type` keyword
    //
    // Example:
    // open System
    //
    // type String with
    //     member this.MyExtensionMethod() =
    //         printfn "hi"
    //
    member this.TransformNode(declaration: IDeclaration) =
        match declaration with
        | :? ITypeExtensionDeclaration as typeExtension -> typeExtension.Parent
        | _ -> declaration