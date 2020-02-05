[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Util.OpensUtil

open JetBrains.Application.Settings
open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree

let rec getModuleToOpen (typeElement: ITypeElement): IClrDeclaredElement =
    match typeElement.GetContainingType() with
    | null ->
        typeElement.GetContainingNamespace() :> _

    | containingType ->
        if containingType.IsModule() && containingType.GetAccessType() = ModuleMembersAccessKind.Normal then
            containingType :> _
        else
            getModuleToOpen containingType

let findModuleToInsert (fsFile: IFSharpFile) (offset: DocumentOffset) (settings: IContextBoundSettingsStore) =
    if not (settings.GetValue(fun key -> key.TopLevelOpenCompletion)) then
        fsFile.GetNode<IModuleLikeDeclaration>(offset)
    else
        match fsFile.GetNode<ITopLevelModuleLikeDeclaration>(offset) with
        | null -> fsFile.GetNode<IAnonModuleDeclaration>(offset) :> _
        | moduleDecl -> moduleDecl :> _

let tryGetFirstOpensGroup (moduleDecl: IModuleLikeDeclaration) =
    let opens =
        moduleDecl.MembersEnumerable
        |> Seq.takeWhile (fun m -> m :? IOpenStatement)
        |> Seq.cast<IOpenStatement>
        |> List.ofSeq

    if opens.IsEmpty then None else Some opens

let isSystemNs ns =
    ns = "System" || startsWith "System." ns

let canInsertBefore ns (openStatement: IOpenStatement) =
    if isSystemNs ns && not openStatement.IsSystem then true else

    ns < openStatement.ReferenceName.QualifiedName

let addOpen (offset: DocumentOffset) (fsFile: IFSharpFile) (settings: IContextBoundSettingsStore) (ns: string) =
    let elementFactory = fsFile.CreateElementFactory()
    let lineEnding = fsFile.GetLineEnding()

    let insertBeforeModuleMember (moduleMember: IModuleMember) =
        let indent = moduleMember.Indent
        addNodesBefore moduleMember [
            // todo: add only if needed
            // Add space before new opens group.
            if not (moduleMember :? IOpenStatement) then
                NewLine(lineEnding)

            if indent > 0 then
                Whitespace(indent)
            elementFactory.CreateOpenStatement(ns)
            NewLine(lineEnding)

            // Add space after new opens group.
            if not (moduleMember :? IOpenStatement) && not (isFollowedByEmptyLine moduleMember) then
                NewLine(lineEnding)
        ] |> ignore

    let insertAfterAnchor (anchor: ITreeNode) indent =
        addNodesAfter anchor [
            if not (anchor :? IOpenStatement) then
                NewLine(lineEnding)
            NewLine(lineEnding)
            if indent > 0 then
                Whitespace(indent)
            elementFactory.CreateOpenStatement(ns)
            if not (anchor :? IOpenStatement) && not (isFollowedByEmptyLine anchor) then
                NewLine(lineEnding)
        ] |> ignore

    let rec addOpenToOpensGroup (opens: IOpenStatement list) =
        match opens with
        | [] -> failwith "Expecting non-empty list"
        | openStatement :: rest ->

        if canInsertBefore ns openStatement then
            insertBeforeModuleMember openStatement
        else
            match rest with
            | [] -> insertAfterAnchor openStatement openStatement.Indent
            | _ -> addOpenToOpensGroup rest

    let moduleDecl = findModuleToInsert fsFile offset settings
    match tryGetFirstOpensGroup moduleDecl with
    | Some opens -> addOpenToOpensGroup opens
    | _ ->

    match Seq.tryHead moduleDecl.MembersEnumerable with
    | None -> failwith "Expecting any module member"
    | Some firstModuleMember ->

    match moduleDecl with
    | :? IAnonModuleDeclaration ->
        // Skip all leading comments and add a new opens group before the first existing member.
        insertBeforeModuleMember firstModuleMember
    | _ ->

    let indent = firstModuleMember.Indent

    let anchor =
        match moduleDecl with
        | :? INestedModuleDeclaration as nestedModule -> nestedModule.EqualsToken :> ITreeNode
        | :? ITopLevelModuleLikeDeclaration as moduleDecl -> moduleDecl.NameIdentifier :> _
        | _ -> failwithf "Unexpected module: %O" moduleDecl

    insertAfterAnchor anchor indent
