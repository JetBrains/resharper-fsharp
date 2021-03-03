namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Application.Progress
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

[<AllowNullLiteral>]
type FSharpGeneratorContext(kind, typeDecl: IFSharpTypeDeclaration) =
    inherit GeneratorContextBase(kind)

    member x.TypeDeclaration = typeDecl

    override x.Language = FSharpLanguage.Instance :> _

    override x.Root = typeDecl :> _
    override val Anchor = null with get, set // todo

    override x.PsiModule = typeDecl.GetPsiModule()
    override x.Project = typeDecl.GetProject()

    override x.GetSelectionTreeRange() = TreeTextRange.InvalidRange // todo

    override x.CreatePointer() =
        FSharpGeneratorWorkflowPointer(x) :> _

    static member Create(kind, treeNode: ITreeNode) =
        if isNull treeNode || treeNode.IsFSharpSigFile() then null else

        let typeDeclaration = treeNode.As<IFSharpTypeDeclaration>()
        if isNull typeDeclaration || isNull typeDeclaration.DeclaredElement then null else

        FSharpGeneratorContext(kind, typeDeclaration)


and FSharpGeneratorWorkflowPointer(context: FSharpGeneratorContext) =
    interface IGeneratorContextPointer with
        // todo: use actual pointers
        member x.TryRestoreContext() = context :> _


[<Language(typeof<FSharpLanguage>)>]
type FSharpGeneratorContextFactory() =
    interface IGeneratorContextFactory with
        member x.TryCreate(kind: string, psiDocumentRangeView: IPsiDocumentRangeView): IGeneratorContext =
            let psiView = psiDocumentRangeView.View<FSharpLanguage>()
            let typeDeclaration: IFSharpTypeDeclaration =
                match psiView.GetSelectedTreeNode<IFSharpTypeDeclaration>() with
                | null ->
                    match psiView.GetSelectedTreeNode<ITypeDeclarationGroup>() with
                    | null -> null
                    | group -> group.TypeDeclarations.FirstOrDefault().As<IFSharpTypeDeclaration>()
                | typeDeclaration -> typeDeclaration

            FSharpGeneratorContext.Create(kind, typeDeclaration) :> _

        member x.TryCreate(kind, treeNode, _) =
            FSharpGeneratorContext.Create(kind, treeNode) :> _

        member x.TryCreate(_: string, _: IDeclaredElement): IGeneratorContext = null


type FSharpGeneratorElement(element, mfv, displayContext, substitution, addTypes) =
    inherit GeneratorDeclaredElement(element)

    new (element, mfvInstance: FcsMfvInstance, addTypes) =
        FSharpGeneratorElement(element, mfvInstance.Mfv, mfvInstance.DisplayContext, mfvInstance.Substitution, addTypes)

    member x.Member = element

    interface IFSharpGeneratorElement with
        member x.Mfv = mfv
        member x.DisplayContext = displayContext
        member x.Substitution = substitution
        member x.AddTypes = addTypes
        member x.IsOverride = true

    override x.ToString() = element.ToString()


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
        let typeDeclaration = context.TypeDeclaration
        let typeElement = typeDeclaration.DeclaredElement
        if not (canHaveOverrides typeElement) then () else

        let fcsEntity = typeDeclaration.GetFSharpSymbol().As<FSharpEntity>()
        if isNull fcsEntity then () else

        let displayContext = typeDeclaration.GetFSharpSymbolUse().DisplayContext

        let rec getBaseTypes (fcsEntity: FSharpEntity) =
            let rec loop acc (fcsType: FSharpType) =
                let acc = FcsEntityInstance.create fcsType :: acc

                match fcsType.BaseType with
                | Some baseType when baseType.HasTypeDefinition -> loop acc baseType
                | _ -> List.rev acc

            match fcsEntity.BaseType with
            | Some baseType when baseType.HasTypeDefinition -> loop [] baseType
            | _ -> []

        let ownMembersIds =
            typeElement.GetMembers()
            |> Seq.filter (fun e -> not (e :? IFSharpGeneratedElement))
            |> Seq.map getTestDescriptor
            |> HashSet

        let memberInstances =
            GenerateUtil.GetOverridableMembersOrder(typeElement, false)
            |> Seq.map (fun i -> i.Member.XMLDocId, i)
            |> dict

        let baseFcsTypes = getBaseTypes fcsEntity

        let baseFcsMembers =
            baseFcsTypes |> List.map (fun fcsEntityInstance ->
                let mfvInstances =
                    fcsEntityInstance.Entity.MembersFunctionsAndValues
                    |> Seq.map (fun mfv -> FcsMfvInstance.create mfv displayContext fcsEntityInstance.Substitution)
                    |> Seq.toList
                fcsEntityInstance, mfvInstances)

        let alreadyOverriden = HashSet()

        let overridableMemberInstances =
            baseFcsMembers |> List.collect (fun (_, mfvInstances) ->
                mfvInstances |> List.choose (fun mfvInstance ->
                    let mfv = mfvInstance.Mfv
                    if mfv.IsAccessor() then None else // todo: allow generating accessors

                    // FCS provides wrong XmlDocId for accessors, e.g. T.P for T.get_P()
                    let xmlDocId = mfv.GetXmlDocId()
                    if ownMembersIds.Contains(xmlDocId) then None else

                    let mutable memberInstance = Unchecked.defaultof<_>
                    if not (memberInstances.TryGetValue(xmlDocId, &memberInstance)) then None else
                    if alreadyOverriden.Contains(memberInstance) then None else

                    OverridableMemberImpl.GetImmediateOverride(memberInstance) |> alreadyOverriden.AddRange
                    Some (memberInstance.Member, mfvInstance))
                |> Seq.toList)

        let needsTypesAnnotations = 
            overridableMemberInstances
            |> List.distinctBy (fst >> getTestDescriptor)
            |> List.map snd
            |> GenerateOverrides.getMembersNeedingTypeAnnotations

        let missingMembersOnly = context.Kind = GeneratorStandardKinds.MissingMembers

        overridableMemberInstances
        |> Seq.filter (fun (m, _) ->
            // todo: events, anything else?
            // todo: separate getters/setters (including existing ones)
            (m :? IMethod || m :? IProperty || m :? IEvent) && m.CanBeOverridden())
        |> Seq.map (fun (m, mfvInstance) ->
            FSharpGeneratorElement(m, mfvInstance, needsTypesAnnotations.Contains(mfvInstance.Mfv)))
        |> Seq.filter (fun i -> not (ownMembersIds.Contains(i.TestDescriptor)))
        |> Seq.distinctBy (fun i -> i.TestDescriptor) // todo: better way to check shadowing/overriding members
        |> Seq.filter (fun i -> not missingMembersOnly || i.Member.IsAbstract)
        |> Seq.iter context.ProvidedElements.Add


[<GeneratorBuilder(GeneratorStandardKinds.Overrides, typeof<FSharpLanguage>)>]
[<GeneratorBuilder(GeneratorStandardKinds.MissingMembers, typeof<FSharpLanguage>)>]
type FSharpOverridingMembersBuilder() =
    inherit GeneratorBuilderBase<FSharpGeneratorContext>()

    override x.Process(context: FSharpGeneratorContext, _: IProgressIndicator) =
        use writeCookie = WriteLockCookie.Create(true)
        use disableFormatter = new DisableCodeFormatter()

        let typeDecl = context.Root :?> IFSharpTypeDeclaration

        let anchor: ITreeNode =
            let typeMembers = typeDecl.TypeMembers
            if not typeMembers.IsEmpty then typeMembers.Last() :> _ else

            let typeRepr = typeDecl.TypeRepresentation.NotNull()
            let objModelTypeRepr = typeRepr.As<IObjectModelTypeRepresentation>()
            if isNull objModelTypeRepr then typeRepr :> _ else

            let typeMembers = objModelTypeRepr.TypeMembers
            if not typeMembers.IsEmpty then typeMembers.Last() :> _ else

            if objModelTypeRepr :? IStructRepresentation then objModelTypeRepr :> _ else

            let equalsToken = typeDecl.EqualsToken.NotNull()

            let anchor =
                let afterComment = getLastMatchingNodeAfter isInlineSpaceOrComment equalsToken
                let afterSpace = getLastMatchingNodeAfter isInlineSpace equalsToken
                if afterComment != afterSpace then afterComment else equalsToken :> _

            deleteChildRange anchor.NextSibling typeRepr

            equalsToken :> _

        let (anchor: ITreeNode), indent =
            match anchor with
            | :? IStructRepresentation as structRepr ->
                structRepr.StructKeyword :> _, structRepr.StructKeyword.Indent + typeDecl.GetIndentSize()
            | :? ITokenNode ->
                let typeDeclarationGroup = TypeDeclarationGroupNavigator.GetByTypeDeclaration(typeDecl).NotNull()
                anchor, typeDeclarationGroup.Indent + typeDecl.GetIndentSize()
            | _ -> anchor, anchor.Indent

        let anchor = GenerateOverrides.addEmptyLineIfNeeded anchor

        context.InputElements
        |> Seq.cast
        |> Seq.map (GenerateOverrides.generateMember typeDecl indent)
        |> Seq.collect (withNewLineAndIndentBefore indent)
        |> addNodesAfter anchor
        |> ignore
