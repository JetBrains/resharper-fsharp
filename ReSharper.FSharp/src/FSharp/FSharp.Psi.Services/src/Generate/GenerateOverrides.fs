[<RequireQualifiedAccess>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate.GenerateOverrides

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Application.Settings
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util

let getMembersNeedingTypeAnnotations (mfvInstances: FcsMfvInstance list) =
    let sameParamNumberMembersGroups =
        mfvInstances
        |> List.map (fun mfvInstance -> mfvInstance.Mfv)
        |> List.groupBy (fun mfv ->
            mfv.LogicalName, Seq.map Seq.length mfv.CurriedParameterGroups |> Seq.toList)

    let sameParamNumberMembers =
        List.map snd sameParamNumberMembersGroups

    sameParamNumberMembers
    |> Seq.filter (Seq.length >> ((<) 1))
    |> Seq.concat
    |> HashSet

let generateMember (context: IFSharpTreeNode) (indent: int) (element: IFSharpGeneratorElement) =
    let mfv = element.Mfv
    let displayContext = element.DisplayContext
    let addTypes = element.AddTypes

    Assertion.Assert(not (mfv.IsNonCliEventProperty()))

    let mutable nextUnnamedVariableNumber = 0
    let getUnnamedVariableName () =
        let name = sprintf "var%d" nextUnnamedVariableNumber
        nextUnnamedVariableNumber <- nextUnnamedVariableNumber + 1
        name

    let isPropertyGetterMethod = mfv.IsPropertyGetterMethod
    let isPropertySetterMethod = mfv.IsPropertySetterMethod
    let isPropertyAccessor = isPropertyGetterMethod || isPropertySetterMethod

    let paramGroups = mfv.CurriedParameterGroups

    let argNames =
        let getParamType (param: FSharpParameter) =
            param.Type.Instantiate(element.Substitution)

        let getParamNameAndType defaultName (param: FSharpParameter) =
            param.Name |> Option.defaultWith defaultName, getParamType param

        let getValueParamNameAndType (param: FSharpParameter) =
            getParamNameAndType (fun _ -> "value") param

        if isPropertySetterMethod && paramGroups.Count = 1 && paramGroups[0].Count > 0 then
            let paramGroup = paramGroups[0]
            if paramGroup.Count = 1 then
                [ [ getValueParamNameAndType paramGroup[0] ] ]
            else
                let accessorParams, valueParam =
                    paramGroup
                    |> List.ofSeq
                    |> List.splitAt (paramGroup.Count - 1)

                [ accessorParams |> List.map (getParamNameAndType getUnnamedVariableName)
                  valueParam |> List.map getValueParamNameAndType ]
        else
            paramGroups
            |> Seq.map (Seq.map (getParamNameAndType getUnnamedVariableName) >> Seq.toList)
            |> Seq.toList

    let typeParams =
        if not addTypes then [] else
        mfv.GenericParameters |> Seq.map (fun param -> param.Name) |> Seq.toList

    let memberName =
        if isPropertyAccessor then
            mfv.LogicalName.Substring(4)
        else
            mfv.LogicalName

    let factory = context.CreateElementFactory()
    let settingsStore = context.GetSettingsStoreWithEditorConfig()
    let spaceAfterComma = settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceAfterComma)

    let generateParameters =
        if isPropertySetterMethod then true else
        if isPropertyGetterMethod && paramGroups.Count = 1 && paramGroups[0].Count = 0 then false else

        not (mfv.IsCliEvent())

    let paramGroups =
        if not generateParameters then [] else
        factory.CreateMemberParamDeclarations(argNames, spaceAfterComma, addTypes, isPropertyAccessor, displayContext)

    let memberDeclaration =
        if isPropertyAccessor && generateParameters then
            let accessorName = if isPropertyGetterMethod then "get" else "set"
            factory.CreatePropertyWithAccessor(memberName, accessorName, paramGroups)
        else
            factory.CreateMemberBindingExpr(memberName, typeParams, paramGroups)

    if element.IsOverride then
        memberDeclaration.SetOverride(true)

    if element.Mfv.IsCliEvent() then
        let attribute = context.CreateElementFactory().CreateAttribute("CLIEvent")
        FSharpAttributesUtil.addOuterAttributeListWithIndent true indent memberDeclaration
        FSharpAttributesUtil.addAttribute memberDeclaration.AttributeLists[0] attribute |> ignore

    if addTypes then
        let lastParam = memberDeclaration.ParametersDeclarations.LastOrDefault()
        if isNull lastParam then () else

        let typeString = mfv.ReturnParameter.Type.Instantiate(element.Substitution)
        let typeUsage = factory.CreateTypeUsage(typeString.Format(displayContext), TypeUsageContext.TopLevel)
        ModificationUtil.AddChildAfter(lastParam, factory.CreateReturnTypeInfo(typeUsage)) |> ignore

    memberDeclaration

let noEmptyLineAnchors =
    NodeTypeSet(
        FSharpTokenType.STRUCT,
        FSharpTokenType.CLASS,
        FSharpTokenType.WITH,
        FSharpTokenType.EQUALS,
        FSharpTokenType.LINE_COMMENT)

let getThisOrPreviousMeaningfulSibling (node: ITreeNode) =
    if isNotNull node && node.IsFiltered() then node.GetPreviousMeaningfulSibling() else node

let addEmptyLineBeforeIfNeeded (anchor: ITreeNode) =
    let addEmptyLine =
        not noEmptyLineAnchors[getTokenType anchor] &&

        let anchor = getThisOrPreviousMeaningfulSibling anchor
        not ((anchor :? IOverridableMemberDeclaration) && anchor.IsSingleLine)

    if addEmptyLine then
        let anchor = getLastMatchingNodeAfter isInlineSpaceOrComment anchor
        ModificationUtil.AddChildAfter(anchor, NewLine(anchor.GetLineEnding())) :> ITreeNode
    else
        anchor
    |> getLastMatchingNodeAfter isInlineSpaceOrComment

let addSpaceAfterIfNeeded (lastGeneratedNode: ITreeNode) =
    if isBeforeEmptyLine lastGeneratedNode then () else

    let nextNode = lastGeneratedNode.GetNextMeaningfulSibling()
    if nextNode :? ITypeBodyMemberDeclaration && not nextNode.IsSingleLine then
        addNodeAfter lastGeneratedNode (NewLine(lastGeneratedNode.GetLineEnding()))

    if getTokenType lastGeneratedNode.NextSibling == FSharpTokenType.RBRACE then
        addNodeAfter lastGeneratedNode (Whitespace())

let getGeneratedSelectionTreeRange (lastNode: ITreeNode) (generatedNodes: seq<ITreeNode>) =
    generatedNodes
    |> Seq.takeWhile (fun node -> node.GetTreeEndOffset().Offset <= lastNode.GetTreeEndOffset().Offset)
    |> Seq.choose (fun node ->
           match node with
           | :? IMemberDeclaration as memberDecl -> Some(memberDecl)
           | _ -> None)
    |> Seq.tryLast
    |> Option.map (fun memberDecl ->
        match memberDecl.AccessorDeclarationsEnumerable |> Seq.tryHead with
        | Some accessorDecl -> accessorDecl.Expression.GetTreeTextRange()
        | _ -> memberDecl.Expression.GetTreeTextRange())
    |> Option.defaultValue TreeTextRange.InvalidRange

let private getObjectTypeReprAnchor (objectTypeRepr: IObjectModelTypeRepresentation) (psiView: IPsiView) =
    let node = psiView.GetSelectedTreeNode()
    if node.GetTreeStartOffset().Offset < objectTypeRepr.GetTreeStartOffset().Offset then null else

    let prevSibling = getThisOrPreviousMeaningfulSibling node
    if isNotNull prevSibling && prevSibling != objectTypeRepr.EndKeyword then prevSibling else null

let canInsertBefore (node: ITreeNode) =
    match node with
    | null -> true
    | :? ILetBindingsDeclaration
    | :? IDoStatement
    | :? IValFieldDeclaration
    | :? ITypeInherit -> false
    | _ -> true

let canInsertAtNode (node: ITreeNode) =
    isNotNull node && isAtEmptyLine node &&
    canInsertBefore (node.GetNextMeaningfulSibling())

let getAnchorNode (psiView: IPsiView) (typeDecl: IFSharpTypeElementDeclaration): ITreeNode =
    let memberDecl = psiView.GetSelectedTreeNode<ITypeBodyMemberDeclaration>()
    if isNotNull memberDecl && canInsertBefore (memberDecl.GetNextMeaningfulSibling()) then memberDecl else

    let selectedTreeNode = psiView.GetSelectedTreeNode()
    if canInsertAtNode selectedTreeNode then
        if isNull typeDecl || typeDecl.Contains(selectedTreeNode) then selectedTreeNode else
        typeDecl.FindLastTokenIn().GetPreviousMeaningfulToken(true)
    else
        let objectTypeRepr = psiView.GetSelectedTreeNode<IObjectModelTypeRepresentation>()
        if isNotNull objectTypeRepr then
            getObjectTypeReprAnchor objectTypeRepr psiView else

        let typeRepresentation = psiView.GetSelectedTreeNode<ITypeRepresentation>()
        if isNotNull typeRepresentation then typeRepresentation else

        let selectedTreeNode = psiView.GetSelectedTreeNode()
        selectedTreeNode.LeftSiblings()
        |> Seq.tryFind (fun node -> node :? ITypeBodyMemberDeclaration || node :? ITypeRepresentation)
        |> Option.defaultValue null

let canHaveOverrides (typeElement: ITypeElement) =
    // todo: filter out union cases
    match typeElement with
    | :? FSharpClass as fsClass -> not (fsClass.IsAbstract && fsClass.IsSealed)
    | :? IStruct -> true
    | _ -> false // todo: interfaces with default impl

let getTestDescriptor (overridableMember: ITypeMember) =
    GeneratorElementBase.GetTestDescriptor(overridableMember, overridableMember.IdSubstitution)

let getOverridableMembers (typeDeclaration: IFSharpTypeElementDeclaration) missingMembersOnly : FSharpGeneratorElement seq =
    if isNull typeDeclaration then [] else

    let typeElement = typeDeclaration.DeclaredElement
    if not (canHaveOverrides typeElement) then [] else

    let psiModule = typeElement.Module

    let fcsSymbol, fcsSymbolUse =
        match typeDeclaration with
        | :? IObjExpr as objExpr ->
            let reference = objExpr.TypeName.Reference
            reference.GetFcsSymbol(), reference.GetSymbolUse()
        | _ -> typeDeclaration.GetFcsSymbol(), typeDeclaration.GetFcsSymbolUse()

    if isNull fcsSymbol then [] else

    let fcsEntity = fcsSymbol.As<FSharpEntity>()
    if isNull fcsEntity then [] else

    let displayContext = fcsSymbolUse.DisplayContext

    let rec getBaseTypes includeThis (fcsEntity: FSharpEntity) =
        let rec loop acc (fcsType: FSharpType) =
            let fcsEntityInstance = FcsEntityInstance.create fcsType
            let acc = if isNotNull fcsEntityInstance then fcsEntityInstance :: acc else acc

            match fcsType.BaseType with
            | Some baseType when baseType.HasTypeDefinition -> loop acc baseType
            | _ -> List.rev acc

        let acc =
            if not includeThis then [] else

            [FcsEntityInstance.create (fcsEntity.AsType())]

        match fcsEntity.BaseType with
        | Some baseType when baseType.HasTypeDefinition -> loop acc baseType
        | _ -> acc

    let ownMembersDescriptors =
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

    let isObjExpr = typeDeclaration :? IObjExpr
    let baseFcsTypes = getBaseTypes isObjExpr fcsEntity

    let baseFcsMembers =
        baseFcsTypes |> List.map (fun fcsEntityInstance ->
            let mfvInstances =
                fcsEntityInstance.Entity.MembersFunctionsAndValues
                |> Seq.map (fun mfv -> FcsMfvInstance.create mfv displayContext fcsEntityInstance.Substitution)
                |> Seq.toList
            fcsEntityInstance, mfvInstances)

    let alreadyOverriden = Dictionary<OverridableMemberInstance, PropertyOverrideState>()

    let addOverrides (memberInstance: OverridableMemberInstance) =
        for overridableMemberInstance in OverridableMemberImpl.GetImmediateOverride(memberInstance) do
            let state =
                match memberInstance.Member with
                | :? IProperty as prop ->
                    (if prop.IsReadable then PropertyOverrideState.Getter else PropertyOverrideState.None) |||
                    (if prop.IsReadable then PropertyOverrideState.Getter else PropertyOverrideState.None)
                | _ -> PropertyOverrideState.None

            let state =
                match alreadyOverriden.TryGetValue(overridableMemberInstance) with
                | true, existingState -> existingState ||| state
                | _ -> state

            alreadyOverriden[overridableMemberInstance] <- state

    let isOverridden (memberInstance: OverridableMemberInstance) =
        match alreadyOverriden.TryGetValue(memberInstance) with
        | false, _ -> false
        | true, state ->

        let prop = memberInstance.Member.As<IProperty>()
        isNull prop ||

        (not prop.IsReadable || (state &&& PropertyOverrideState.Getter <> enum 0)) &&
        (not prop.IsWritable || (state &&& PropertyOverrideState.Setter <> enum 0))

    for KeyValue(_, memberInstance) in memberInstances do
        let fsTypeMember = memberInstance.Member.As<IFSharpTypeMember>()
        if isNull fsTypeMember || fsTypeMember.IsVisibleFromFSharp then
            addOverrides memberInstance

    let overridableMemberInstances =
        baseFcsMembers |> List.collect (fun (_, mfvInstances) ->
            mfvInstances |> List.choose (fun mfvInstance ->
                let mfv = mfvInstance.Mfv
                if mfv.IsAccessor() then None else

                let xmlDocId =
                    match mfv.GetDeclaredElement(psiModule).As<ITypeMember>() with
                    | null -> mfv.GetXmlDocId()
                    | typeMember -> XMLDocUtil.GetTypeMemberXmlDocId(typeMember, typeMember.ShortName)

                if ownMembersDescriptors.Contains(xmlDocId) then None else

                let mutable memberInstance = Unchecked.defaultof<_>
                if not (memberInstances.TryGetValue(xmlDocId, &memberInstance)) then None else

                if isOverridden memberInstance then None else

                addOverrides memberInstance
                Some (memberInstance.Member, mfvInstance)
            )
            |> Seq.toList
        )

    let needsTypesAnnotations =
        overridableMemberInstances
        |> List.distinctBy (fst >> getTestDescriptor)
        |> List.map snd
        |> getMembersNeedingTypeAnnotations

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
    |> Seq.filter (fun i -> not (ownMembersDescriptors.Contains(i.TestDescriptor)))
    |> Seq.distinctBy (fun i -> i.TestDescriptor) // todo: better way to check shadowing/overriding members
    |> Seq.filter (fun i -> not missingMembersOnly || i.Member.IsAbstract)
