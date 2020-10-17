namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features

open System.Collections.Generic
open FSharp.Compiler.SourceCodeServices
open JetBrains.Application.Progress
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

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
            // todo: get union through union case
            let typeDeclaration = psiView.GetSelectedTreeNode<IFSharpTypeDeclaration>()
            FSharpGeneratorContext.Create(kind, typeDeclaration) :> _

        member x.TryCreate(_, _, _) = null
        member x.TryCreate(_: string, _: IDeclaredElement): IGeneratorContext = null


type FSharpGeneratorElement(element, mfv, substitution, addTypes) =
    inherit GeneratorDeclaredElement(element)

    interface IFSharpGeneratorElement with
        member x.Mfv = mfv
        member x.Substitution = substitution
        member x.AddTypes = addTypes

    override x.ToString() = element.ToString()


[<GeneratorElementProvider(GeneratorStandardKinds.Overrides, typeof<FSharpLanguage>)>]
type FSharpOverridableMembersProvider() =
    inherit GeneratorProviderBase<FSharpGeneratorContext>()

    let canHaveOverrides (typeElement: ITypeElement) =
        // todo: filter out union cases
        match typeElement with
        | :? FSharpClass as fsClass -> not (fsClass.IsAbstract && fsClass.IsSealed)
        | :? IStruct -> true
        | _ -> false // todo: interfaces with default impl

    override x.Populate(context: FSharpGeneratorContext) =
        let typeDeclaration = context.TypeDeclaration
        let typeElement = typeDeclaration.DeclaredElement
        if not (canHaveOverrides typeElement) then () else

        let fcsEntity = typeDeclaration.GetFSharpSymbol().As<FSharpEntity>()
        if isNull fcsEntity then () else

        let rec getBaseTypes (fcsEntity: FSharpEntity) =
            let rec loop acc (fcsType: FSharpType) =
                let fcsType = getAbbreviatedType fcsType
                let fcsEntity = fcsType.TypeDefinition
                let substitution = Seq.zip fcsEntity.GenericParameters fcsType.GenericArguments |> Seq.toList
                let acc = (fcsEntity, substitution) :: acc

                match fcsType.BaseType with
                | Some baseType when baseType.HasTypeDefinition -> loop acc baseType
                | _ -> List.rev acc

            match fcsEntity.BaseType with
            | Some baseType when baseType.HasTypeDefinition -> loop [] baseType
            | _ -> []

        let memberInstances =
            GenerateUtil.GetOverridableMembersOrder(typeElement, false)
            |> Seq.map (fun i -> i.Member.XMLDocId, i)
            |> dict

        let memberInstances =
            let baseTypes = getBaseTypes fcsEntity
            baseTypes |> Seq.collect (fun (fcsEntity, substitution) ->
                fcsEntity.MembersFunctionsAndValues |> Seq.choose (fun mfv ->
                    match memberInstances.TryGetValue(mfv.XmlDocSig) with
                    | true, i -> Some (i.Member, (mfv, substitution))
                    | _ -> None))
            |> Seq.toList

        let needsTypesAnnotations = 
            let sameParamNumberMembersGroups = 
                memberInstances
                |> Seq.distinctBy (fun (m, _) -> GeneratorElementBase.GetTestDescriptor(m, m.IdSubstitution))
                |> Seq.groupBy (fun (_, (mfv, _)) ->
                    let parameterGroups = mfv.CurriedParameterGroups
                    mfv.LogicalName, (Seq.length parameterGroups), (Seq.map Seq.length parameterGroups |> Seq.toList))
                |> Seq.toList

            let sameParamNumberMembers =
                List.map (snd >> (Seq.map snd) >> Seq.toList) sameParamNumberMembersGroups

            sameParamNumberMembers
            |> Seq.filter (Seq.length >> ((<) 1))
            |> Seq.concat
            |> Seq.map fst
            |> HashSet

        memberInstances
        |> Seq.filter (fun (m, _) ->
            // todo: events, anything else?
            // todo: separate getters/setters (including existing ones)
            (m :? IMethod || m :? IProperty) && m.GetContainingType() <> typeElement && m.CanBeOverridden())
        |> Seq.map (fun (i, (mfv, substitution)) -> FSharpGeneratorElement(i, mfv, substitution, needsTypesAnnotations.Contains(mfv)))
        |> Seq.distinctBy (fun i -> i.TestDescriptor) // todo: better way to check shadowing/overriding members
        |> Seq.iter context.ProvidedElements.Add


[<GeneratorBuilder(GeneratorStandardKinds.Overrides, typeof<FSharpLanguage>)>]
type FSharpOverridingMembersBuilder() =
    inherit GeneratorBuilderBase<FSharpGeneratorContext>()

    override x.Process(context: FSharpGeneratorContext, _: IProgressIndicator) =
        use writeCookie = WriteLockCookie.Create(true)
        use disableFormatter = new DisableCodeFormatter()

        let typeDecl = context.Root :?> IFSharpTypeDeclaration
        let displayContext = typeDecl.GetFSharpSymbolUse().DisplayContext

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
            deleteChildRange equalsToken.NextSibling typeRepr

            equalsToken :> _

        let (anchor: ITreeNode), indent =
            match anchor with
            | :? IStructRepresentation as structRepr ->
                structRepr.StructKeyword :> _, structRepr.StructKeyword.Indent + typeDecl.GetIndentSize()
            | :? ITokenNode ->
                let typeDeclarationGroup = TypeDeclarationGroupNavigator.GetByTypeDeclaration(typeDecl).NotNull()
                anchor, typeDeclarationGroup.Indent + typeDecl.GetIndentSize()
            | _ -> anchor, anchor.Indent

        context.InputElements
        |> Seq.map (fun input ->
            let element = input :?> FSharpGeneratorElement
            let memberDecl = GenerateOverrides.generateMember typeDecl displayContext element
            memberDecl.SetOverride(true)
            memberDecl)
        |> Seq.collect (withNewLineAndIndentBefore indent)
        |> addNodesAfter anchor |> ignore
