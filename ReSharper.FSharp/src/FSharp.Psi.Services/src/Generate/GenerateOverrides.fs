module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate.GenerateOverrides

open System.Collections.Generic
open JetBrains.Application.Settings
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

    let mutable nextUnnamedVariableNumber = 0
    let getUnnamedVariableName () =
        let name = sprintf "var%d" nextUnnamedVariableNumber
        nextUnnamedVariableNumber <- nextUnnamedVariableNumber + 1
        name

    let argNames =
        mfv.CurriedParameterGroups
        |> Seq.map (Seq.map (fun x ->
            let name = x.Name |> Option.defaultWith (fun _ -> getUnnamedVariableName ())
            name, x.Type.Instantiate(element.Substitution)) >> Seq.toList)
        |> Seq.toList

    let typeParams =
        if not element.AddTypes then [] else
        mfv.GenericParameters |> Seq.map (fun param -> param.Name) |> Seq.toList

    let memberName = mfv.LogicalName

    let factory = context.CreateElementFactory()
    let settingsStore = context.GetSettingsStoreWithEditorConfig()
    let spaceAfterComma = settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceAfterComma)

    let paramGroups =
        if mfv.IsProperty then [] else
        factory.CreateMemberParamDeclarations(argNames, spaceAfterComma, element.AddTypes, element.DisplayContext)

    let memberDeclaration = factory.CreateMemberBindingExpr(memberName, typeParams, paramGroups)

    if element.IsOverride then
        memberDeclaration.SetOverride(true)

    if element.Mfv.IsCliEvent() then
        let attribute = context.CreateElementFactory().CreateAttribute("CLIEvent")
        FSharpAttributesUtil.addOuterAttributeListWithIndent true indent memberDeclaration
        FSharpAttributesUtil.addAttribute memberDeclaration.AttributeLists[0] attribute |> ignore

    if element.AddTypes then
        let lastParam = memberDeclaration.ParametersDeclarations.LastOrDefault()
        if isNull lastParam then () else

        let typeString = mfv.ReturnParameter.Type.Instantiate(element.Substitution)
        let typeUsage = factory.CreateTypeUsage(typeString.Format(element.DisplayContext))
        ModificationUtil.AddChildAfter(lastParam, factory.CreateReturnTypeInfo(typeUsage)) |> ignore

    memberDeclaration

let noEmptyLineAnchors =
    NodeTypeSet(
        FSharpTokenType.STRUCT,
        FSharpTokenType.CLASS,
        FSharpTokenType.WITH,
        FSharpTokenType.EQUALS)

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
        | Some accessorDecl -> accessorDecl.GetTreeTextRange()
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

let getAnchorNode (psiView: IPsiView): ITreeNode =
    let memberDecl = psiView.GetSelectedTreeNode<ITypeBodyMemberDeclaration>()
    if isNotNull memberDecl && canInsertBefore (memberDecl.GetNextMeaningfulSibling()) then memberDecl else

    let selectedTreeNode = psiView.GetSelectedTreeNode()
    if canInsertAtNode selectedTreeNode then selectedTreeNode else

    let objectTypeRepr = psiView.GetSelectedTreeNode<IObjectModelTypeRepresentation>()
    if isNotNull objectTypeRepr then
        getObjectTypeReprAnchor objectTypeRepr psiView else

    let typeRepresentation = psiView.GetSelectedTreeNode<ITypeRepresentation>()
    if isNotNull typeRepresentation then typeRepresentation else

    let selectedTreeNode = psiView.GetSelectedTreeNode()
    selectedTreeNode.LeftSiblings()
    |> Seq.tryFind (fun node -> node :? ITypeBodyMemberDeclaration || node :? ITypeRepresentation)
    |> Option.defaultValue null
