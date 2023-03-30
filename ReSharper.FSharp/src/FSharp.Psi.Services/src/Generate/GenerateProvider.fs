namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features

open System.Linq
open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Application.Progress
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpGeneratorContextFactory() =
    interface IGeneratorContextFactory with
        member x.TryCreate(kind: string, psiDocumentRangeView: IPsiDocumentRangeView): IGeneratorContext =
            let psiView = psiDocumentRangeView.View<FSharpLanguage>()
            
            let tryGetPreviousTypeDecl () =
                let selectedTreeNode = psiView.GetSelectedTreeNode<IFSharpTreeNode>()
                match selectedTreeNode.GetPreviousMeaningfulSibling() with
                | :? ITypeDeclarationGroup as group ->
                    group.TypeDeclarations.FirstOrDefault().As<IFSharpTypeDeclaration>()
                | :? IFSharpTypeDeclaration as t -> t
                | _ -> null
            
            let typeDeclaration: IFSharpTypeDeclaration =
                match psiView.GetSelectedTreeNode<IFSharpTypeDeclaration>() with
                | null ->
                    match psiView.GetSelectedTreeNode<ITypeDeclarationGroup>() with
                    | null -> tryGetPreviousTypeDecl ()
                    | group -> group.TypeDeclarations.FirstOrDefault().As<IFSharpTypeDeclaration>()
                | typeDeclaration -> typeDeclaration

            let treeNode = psiView.GetSelectedTreeNode()
            if isNull treeNode then null else

            let anchor = GenerateOverrides.getAnchorNode psiView
            FSharpGeneratorContext.Create(kind, treeNode, typeDeclaration, anchor) :> _

        member x.TryCreate(kind, treeNode, anchor) =
            let typeDecl = treeNode.As<IFSharpTypeDeclaration>()
            FSharpGeneratorContext.Create(kind, treeNode, typeDecl, anchor) :> _

        member x.TryCreate(_: string, _: IDeclaredElement): IGeneratorContext = null


type FSharpGeneratorElement(element, mfvInstance: FcsMfvInstance, addTypes) =
    inherit GeneratorDeclaredElement(element)

    member x.AddTypes = addTypes
    member x.Mfv = mfvInstance.Mfv
    member x.MfvInstance = mfvInstance
    member x.Member = element

    interface IFSharpGeneratorElement with
        member x.Mfv = x.Mfv
        member x.DisplayContext = mfvInstance.DisplayContext
        member x.Substitution = mfvInstance.Substitution
        member x.AddTypes = x.AddTypes
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
        if isNull typeDeclaration then () else

        let typeElement = typeDeclaration.DeclaredElement
        if not (canHaveOverrides typeElement) then () else

        let psiModule = typeElement.Module
        let missingMembersOnly = context.Kind = GeneratorStandardKinds.MissingMembers

        let fcsEntity = typeDeclaration.GetFcsSymbol().As<FSharpEntity>()
        if isNull fcsEntity then () else

        let displayContext = typeDeclaration.GetFcsSymbolUse().DisplayContext

        let rec getBaseTypes (fcsEntity: FSharpEntity) =
            let rec loop acc (fcsType: FSharpType) =
                let fcsEntityInstance = FcsEntityInstance.create fcsType
                let acc = if isNotNull fcsEntityInstance then fcsEntityInstance :: acc else acc

                match fcsType.BaseType with
                | Some baseType when baseType.HasTypeDefinition -> loop acc baseType
                | _ -> List.rev acc

            match fcsEntity.BaseType with
            | Some baseType when baseType.HasTypeDefinition -> loop [] baseType
            | _ -> []

        let ownMembersIds =
            typeElement.GetMembers()
            |> Seq.collect (fun typeMember ->
                if typeMember :? IFSharpGeneratedElement then Seq.empty else
                if not missingMembersOnly then Seq.singleton typeMember else

                match typeMember with
                | :? IProperty as prop -> prop.GetAllAccessors() |> Seq.cast
                | _ -> [typeMember])
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
                    if mfv.IsAccessor() then None else

                    let xmlDocId =
                        match mfv.GetDeclaredElement(psiModule).As<ITypeMember>() with
                        | null -> mfv.GetXmlDocId() 
                        | typeMember -> XMLDocUtil.GetTypeMemberXmlDocId(typeMember, typeMember.ShortName)

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

        overridableMemberInstances
        |> Seq.filter (fun (m, _) -> (m :? IMethod || m :? IProperty || m :? IEvent) && m.CanBeOverridden())
        |> Seq.collect (fun (m, mfvInstance as i) ->
            let mfv = mfvInstance.Mfv
            let prop = m.As<IProperty>()
            if not missingMembersOnly || isNull prop || not (mfv.IsNonCliEventProperty()) then [i] else

            [ if isNotNull prop.Getter && mfv.HasGetterMethod then
                  prop.Getter :> IOverridableMember, { mfvInstance with Mfv = mfv.GetterMethod }
              if isNotNull prop.Setter && mfv.HasSetterMethod then
                  prop.Setter :> IOverridableMember, { mfvInstance with Mfv = mfv.SetterMethod } ])
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

    override this.IsAvailable(context: FSharpGeneratorContext): bool =
        isNotNull context.TypeDeclaration && isNotNull context.TypeDeclaration.DeclaredElement

    override x.Process(context: FSharpGeneratorContext, _: IProgressIndicator) =
        use writeCookie = WriteLockCookie.Create(true)
        use disableFormatter = new DisableCodeFormatter()

        let typeDecl = context.Root :?> IFSharpTypeDeclaration

        match typeDecl.TypeRepresentation with
        | :? IUnionRepresentation as unionRepr ->
            let caseDecl = unionRepr.Cases.FirstOrDefault()
            if isNotNull caseDecl then
                EnumCaseLikeDeclarationUtil.addBarIfNeeded caseDecl
        | _ -> ()

        let anchor: ITreeNode =
            let anchor = context.Anchor
            if isNotNull anchor then anchor else

            let typeMembers = typeDecl.TypeMembers
            if not typeMembers.IsEmpty then typeMembers.Last() :> _ else

            let typeRepr = typeDecl.TypeRepresentation
            if isNull typeRepr then
                typeDecl.EqualsToken.NotNull() else

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
                structRepr.BeginKeyword :> _, structRepr.BeginKeyword.Indent + typeDecl.GetIndentSize()

            | :? ITokenNode as token ->
                let parent = token.Parent
                match parent with
                | :? IObjectModelTypeRepresentation as repr when token != repr.EndKeyword ->
                    let indent =
                        match repr.TypeMembersEnumerable |> Seq.tryHead with
                        | Some memberDecl -> memberDecl.Indent
                        | _ -> repr.BeginKeyword.Indent + typeDecl.GetIndentSize()
                    token, indent
                | _ ->

                let indent = 
                    match typeDecl.TypeMembersEnumerable |> Seq.tryHead with
                    | Some memberDecl -> memberDecl.Indent
                    | _ ->

                    let typeRepr = typeDecl.TypeRepresentation
                    if isNotNull typeRepr then typeRepr.Indent else

                    let typeDeclarationGroup = TypeDeclarationGroupNavigator.GetByTypeDeclaration(typeDecl).NotNull()
                    typeDeclarationGroup.Indent + typeDecl.GetIndentSize()

                anchor, indent

            | _ -> anchor, anchor.Indent

        let anchor =
            if isAtEmptyLine anchor then
                let first = getFirstMatchingNodeBefore isInlineSpace anchor |> getThisOrPrevNewLine
                let last = getLastMatchingNodeAfter isInlineSpace anchor

                let anchor = first.PrevSibling
                deleteChildRange first last
                anchor
            else
                anchor

        let anchor = GenerateOverrides.addEmptyLineBeforeIfNeeded anchor

        let missingMembersOnly = context.Kind = GeneratorStandardKinds.MissingMembers

        let inputElements =
            if missingMembersOnly then context.InputElements |> Seq.cast<FSharpGeneratorElement> else

            context.InputElements
            |> Seq.collect (fun generatorElement ->
                let e = generatorElement :?> FSharpGeneratorElement
                let mfv = e.Mfv
                let prop = e.Member.As<IProperty>()

                if isNull prop || not (mfv.IsNonCliEventProperty()) then [e] else

                [ if isNotNull prop.Getter && mfv.HasGetterMethod then
                      FSharpGeneratorElement(prop.Getter, { e.MfvInstance with Mfv = mfv.GetterMethod }, e.AddTypes)
                  if isNotNull prop.Setter && mfv.HasSetterMethod then
                      FSharpGeneratorElement(prop.Setter, { e.MfvInstance with Mfv = mfv.SetterMethod }, e.AddTypes) ])

        let lastNode = 
            inputElements
            |> Seq.cast
            |> Seq.map (GenerateOverrides.generateMember typeDecl indent)
            |> Seq.collect (withNewLineAndIndentBefore indent)
            |> addNodesAfter anchor

        GenerateOverrides.addEmptyLineAfterIfNeeded lastNode

        let nodes = anchor.RightSiblings()
        let selectedRange = GenerateOverrides.getGeneratedSelectionTreeRange lastNode nodes
        context.SetSelectedRange(selectedRange)
