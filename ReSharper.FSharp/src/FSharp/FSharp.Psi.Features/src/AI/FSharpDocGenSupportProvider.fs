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
            docGenSupportProvider.IsApplicable(declaration) &&

            let docNode = this.FindDocNode(declaration)
            isNotNull docNode && docNode.GetXmlPsi().XmlFile.IsIncompleteDoc()

        member this.IsApplicable(declaration: IDeclaration) =
            declaration :? IAttributesOwnerDeclaration &&
            not (declaration :? IPrimaryConstructorDeclaration)

        member this.CalculateContext(declaration: IDeclaration) =
            let docNode = this.FindDocNode(declaration)
            let declaredElement = declaration.DeclaredElement
            let declaredName = if isNotNull declaredElement then declaredElement.ShortName else declaration.DeclaredName
            GenericContextEntry(declaredName, docNode.GetDocumentRange().TextRange.ToRdTextRange())

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

        member this.UpdateDocUnderTransaction(_, docRange, _) =
            docRange

        member this.FindTarget(node: ITreeNode) =
            node.As<IDeclaration>()

        member this.GetText(declaration: IDeclaration) =
            [ declaration.GetText() ]

        member this.FindDocExample _ = null

    member this.FindDocNode(declaration: IDeclaration) =
        match declaration with
        | :? IFSharpDeclaration as fsDecl -> fsDecl.XmlDocBlock
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
