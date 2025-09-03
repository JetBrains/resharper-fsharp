namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate

open JetBrains.Application.Progress
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<Language(typeof<FSharpLanguage>)>]
type FSharpGeneratorContextFactory() =
    interface IGeneratorContextFactory with
        member x.TryCreate(kind: string, psiDocumentRangeView: IPsiDocumentRangeView): IGeneratorContext =
            let psiView = psiDocumentRangeView.View<FSharpLanguage>()

            let isInsideObjExpr (objExpr: IObjExpr) =
                isNotNull objExpr &&
                not (psiView.SelectionIsExactlyAtEdge(objExpr)) &&
                not (psiView.SelectionIsExactlyAtEdge(objExpr, true))

            let treeNode = psiView.GetSelectedTreeNode()
            if isNull treeNode then null else

            let tryGetPreviousTypeDecl (treeNode: ITreeNode) =
                let prevToken = treeNode.GetPreviousMeaningfulToken()
                if isNull prevToken then null else

                prevToken.GetContainingNode<IFSharpTypeDeclaration>()

            let typeDeclaration: IFSharpTypeElementDeclaration =
                let objExpr = psiView.GetSelectedTreeNode<IObjExpr>()
                if isInsideObjExpr objExpr then objExpr else

                match psiView.GetSelectedTreeNode<IFSharpTypeDeclaration>() with
                | null -> tryGetPreviousTypeDecl treeNode
                | typeDeclaration -> typeDeclaration

            let anchor = GenerateOverrides.getAnchorNode psiView typeDeclaration
            FSharpGeneratorContext.Create(kind, treeNode, typeDeclaration, anchor) :> _

        member x.TryCreate(kind, treeNode, anchor) =
            let typeDecl = treeNode.As<IFSharpTypeElementDeclaration>()
            FSharpGeneratorContext.Create(kind, treeNode, typeDecl, anchor) :> _

        member x.TryCreate(_: string, _: IDeclaredElement): IGeneratorContext = null

[<GeneratorElementProvider(GeneratorStandardKinds.Overrides, typeof<FSharpLanguage>)>]
[<GeneratorElementProvider(GeneratorStandardKinds.MissingMembers, typeof<FSharpLanguage>)>]
type FSharpOverridableMembersProvider() =
    inherit GeneratorProviderBase<FSharpGeneratorContext>()

    let canHaveOverrides (typeElement: ITypeElement) =
        // todo: filter out union cases
        match typeElement with
        | :? FSharpClass as fsClass -> not (fsClass.IsAbstract && fsClass.IsSealed)
        | :? IStruct -> true
        | _ -> false // todo: interfaces with default impl

    let getTestDescriptor (overridableMember: ITypeMember) =
        GeneratorElementBase.GetTestDescriptor(overridableMember, overridableMember.IdSubstitution)

    override x.Populate(context: FSharpGeneratorContext) =
        let missingMembersOnly = context.Kind = GeneratorStandardKinds.MissingMembers
        GenerateOverrides.getOverridableMembers missingMembersOnly context.TypeDeclaration
        |> Seq.iter context.ProvidedElements.Add


[<GeneratorBuilder(GeneratorStandardKinds.Overrides, typeof<FSharpLanguage>)>]
[<GeneratorBuilder(GeneratorStandardKinds.MissingMembers, typeof<FSharpLanguage>)>]
type FSharpOverridingMembersBuilder() =
    inherit GeneratorBuilderBase<FSharpGeneratorContext>()

    let addNewLineBeforeReprIfNeeded (typeDecl: IFSharpTypeDeclaration) (typeRepr: ITypeRepresentation) =
        if isNull typeRepr || typeDecl.Identifier.StartLine <> typeRepr.StartLine then () else

        typeDecl.EqualsToken.AddLineBreakAfter() |> ignore
        typeDecl.TypeRepresentation.FormatNode(CodeFormatProfile.INDENT)
        
    override this.IsAvailable(context: FSharpGeneratorContext): bool =
        let typeDecl = context.TypeDeclaration
        typeDecl :? IObjExpr ||

        isNotNull typeDecl && isNotNull typeDecl.DeclaredElement

    override x.Process(context: FSharpGeneratorContext, _: IProgressIndicator) =
        use writeCookie = WriteLockCookie.Create(true)

        let typeDecl = context.Root :?> IFSharpTypeElementDeclaration

        let (anchor: ITreeNode) =
            match typeDecl with
            | :? IFSharpTypeDeclaration as typeDecl ->
                match typeDecl.TypeRepresentation with
                | :? IUnionRepresentation as unionRepr ->
                    unionRepr.UnionCasesEnumerable
                    |> Seq.tryHead
                    |> Option.iter EnumCaseLikeDeclarationUtil.addBarIfNeeded

                | :? ITypeAbbreviationRepresentation as abbrRepr when abbrRepr.CanBeUnionCase ->
                    let factory = typeDecl.CreateElementFactory()
                    let caseName = FSharpNamingService.mangleNameIfNecessary abbrRepr.AbbreviatedTypeOrUnionCase.SourceName
                    let declGroup = factory.CreateModuleMember($"type U = | {caseName}") :?> ITypeDeclarationGroup
                    let typeDeclaration = declGroup.TypeDeclarations[0] :?> IFSharpTypeDeclaration
                    let repr = typeDeclaration.TypeRepresentation
                    let newRepr = typeDecl.SetTypeRepresentation(repr)
                    if context.Anchor == abbrRepr then context.Anchor <- newRepr

                | _ -> ()

                let typeRepr = typeDecl.TypeRepresentation
                addNewLineBeforeReprIfNeeded typeDecl typeRepr

                let anchor: ITreeNode =
                    let deleteTypeRepr (typeDecl: IFSharpTypeDeclaration) : ITreeNode =
                        let equalsToken = typeDecl.EqualsToken.NotNull()

                        let equalsAnchor =
                            let afterComment = getLastMatchingNodeAfter isInlineSpaceOrComment equalsToken
                            let afterSpace = getLastMatchingNodeAfter isInlineSpace equalsToken
                            if afterComment != afterSpace then afterComment else equalsToken :> _

                        let prev = typeRepr.GetPreviousNonWhitespaceToken()
                        if prev.IsCommentToken() then
                            deleteChildRange prev.NextSibling typeRepr
                            prev
                        else
                            if isNull (typeRepr.GetNextMeaningfulSibling()) then
                                moveCommentsAndWhitespaceInside typeDecl

                            ModificationUtil.DeleteChild(typeRepr)

                            equalsAnchor |> getLastInlineSpaceOrCommentSkipNewLine

                    let anchor =
                        let isEmptyClassRepr =
                            match typeRepr with
                            | :? IClassRepresentation as classRepr ->
                                let classKeyword = classRepr.BeginKeyword
                                let endKeyword = classRepr.EndKeyword

                                isNotNull classKeyword && isNotNull endKeyword &&
                                classKeyword.GetNextNonWhitespaceToken() == endKeyword
                            | _ -> false

                        if isEmptyClassRepr then
                            deleteTypeRepr typeDecl
                        else
                            context.Anchor

                    if isNotNull anchor then anchor else

                    let typeMembers = typeDecl.TypeMembers
                    if not typeMembers.IsEmpty then typeMembers.Last() :> _ else

                    if isNull typeRepr then
                        typeDecl.EqualsToken.NotNull() else

                    let objModelTypeRepr = typeRepr.As<IObjectModelTypeRepresentation>()
                    if isNull objModelTypeRepr then typeRepr :> _ else

                    let typeMembers = objModelTypeRepr.TypeMembers
                    if not typeMembers.IsEmpty then typeMembers.Last() :> _ else

                    objModelTypeRepr

                match anchor with
                | :? IStructRepresentation as structRepr ->
                    structRepr.BeginKeyword :> _

                | :? ITypeRepresentation as typeRepr ->
                    typeRepr

                | treeNode ->
                    let parent =
                        if isNotNull typeRepr && typeRepr.Contains(treeNode) then typeRepr :> ITreeNode else treeNode.Parent
                    match parent with
                    | :? IObjectModelTypeRepresentation as repr when treeNode != repr.EndKeyword ->
                        let doOrLastLet =
                            repr.TypeMembersEnumerable
                            |> Seq.takeWhile (fun x -> x :? ILetBindingsDeclaration || x :? IDoStatement)
                            |> Seq.tryLast

                        match doOrLastLet with
                        | Some node -> node :> ITreeNode
                        | _ -> treeNode
                    | _ ->

                    anchor

            | :? IObjExpr as objExpr ->
                if isNull objExpr.WithKeyword then
                    let node: ITreeNode =
                        match objExpr.ArgExpression with
                        | null -> objExpr.TypeName
                        | argExpr -> argExpr
                    addNodesAfter node [
                        Whitespace()
                        FSharpTokenType.WITH.CreateLeafElement()
                    ] |> ignore

                let memberDeclarations = objExpr.MemberDeclarations

                let anchor: ITreeNode =
                    objExpr.InterfaceImplementationsEnumerable
                    |> Seq.cast
                    |> Seq.tryLast
                    |> Option.orElseWith (fun _ -> memberDeclarations |> Seq.cast |> Seq.tryHead)
                    |> Option.defaultWith (fun _ -> objExpr.WithKeyword)

                anchor

            | typeDecl -> failwith $"Unexpected typeDecl: {typeDecl}"

        let anchor = getLastMatchingNodeAfter isInlineSpaceOrComment anchor

        let missingMembersOnly = context.Kind = GeneratorStandardKinds.MissingMembers

        let inputElements =
            if missingMembersOnly then context.InputElements |> Seq.cast<FSharpGeneratorElement> else

            context.InputElements
            |> Seq.cast<FSharpGeneratorElement>
            |> GenerateOverrides.sanitizeMembers

        let addedMembers = GenerateOverrides.addMembers inputElements typeDecl anchor
        let selectedRange = GenerateOverrides.getGeneratedSelectionTreeRange addedMembers
        context.SetSelectedRange(selectedRange)


[<Language(typeof<FSharpLanguage>)>]
type FSharpInheritanceAnalyzer() =
    interface InheritanceAnalyzer.IInheritanceAnalyzer with
        member this.IInheritanceAnalyzer_GetMissingMembers(typeDeclaration) =
            typeDeclaration.As()
            |> GenerateOverrides.getOverridableMembers true
            |> Seq.map (_.Member >> OverridableMemberInstance)

        member this.IInheritanceAnalyzer_GetOverridableMembers(typeDeclaration) =
            typeDeclaration.As()
            |> GenerateOverrides.getOverridableMembers false
            |> Seq.map (_.Member >> OverridableMemberInstance)
