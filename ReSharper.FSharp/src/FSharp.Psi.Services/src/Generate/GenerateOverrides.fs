module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate.GenerateOverrides

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Application.Settings
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree

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

let addEmptyLineAfterIfNeeded (lastGeneratedNode: ITreeNode) =
    if isBeforeEmptyLine lastGeneratedNode then () else

    let nextNode = lastGeneratedNode.GetNextMeaningfulSibling()
    if nextNode :? ITypeBodyMemberDeclaration && not nextNode.IsSingleLine then
        addNodeAfter lastGeneratedNode (NewLine(lastGeneratedNode.GetLineEnding()))

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

let getAnchorNode (psiView: IPsiView) (typeDecl: IFSharpTypeDeclaration): ITreeNode =
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
