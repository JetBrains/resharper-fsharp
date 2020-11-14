[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Util.OpensUtil

open System.Collections.Generic
open JetBrains.Application.Settings
open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files.SandboxFiles
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

let toQualifiedList (declaredElement: IClrDeclaredElement) =
    let rec loop acc (declaredElement: IClrDeclaredElement) =
        match declaredElement with
        | :? INamespace as ns ->
            if ns.IsRootNamespace then acc else
            loop (declaredElement :: acc) (ns.GetContainingNamespace())

        | :? ITypeElement as typeElement ->
            let acc = declaredElement :: acc

            match typeElement.GetContainingType() with
            | null -> loop acc (typeElement.GetContainingNamespace())
            | containingType -> loop acc containingType

        | _ -> failwithf "Expecting namespace to type element"

    loop [] declaredElement

let getContainingEntity (typeElement: ITypeElement): IClrDeclaredElement =
    match typeElement.GetContainingType() with
    | null -> typeElement.GetContainingNamespace() :> _
    | containingType -> containingType :> _

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

let tryGetOpen (moduleDecl: IModuleLikeDeclaration) namespaceName =
    moduleDecl.MembersEnumerable
    |> Seq.filter (fun m -> m :? IOpenStatement)
    |> Seq.cast<IOpenStatement>
    |> Seq.tryFind (fun x -> x.ReferenceName.QualifiedName = namespaceName)

let removeOpen (openStatement: IOpenStatement) =
    let first = getFirstMatchingNodeBefore isInlineSpaceOrComment openStatement
    let last =
        openStatement
        |> skipSemicolonsAndWhiteSpacesAfter
        |> getThisOrNextNewLine

    deleteChildRange first last

let isSystemNs ns =
    ns = "System" || startsWith "System." ns

let canInsertBefore (openStatement: IOpenStatement) ns =
    if isSystemNs ns then
        not openStatement.IsSystem ||
        ns < openStatement.ReferenceName.QualifiedName
    else
        not openStatement.IsSystem &&
        ns < openStatement.ReferenceName.QualifiedName

let addOpen (offset: DocumentOffset) (fsFile: IFSharpFile) (settings: IContextBoundSettingsStore) (ns: string) =
    let elementFactory = fsFile.CreateElementFactory()
    let lineEnding = fsFile.GetLineEnding()

    let insertBeforeModuleMember (moduleMember: IModuleMember) =
        let indent = moduleMember.Indent

        addNodesBefore moduleMember [
            // todo: add setting for adding space before first module member
            // Add space before new opens group.
            if not (moduleMember :? IOpenStatement) && not (isFirstChildOrAfterEmptyLine moduleMember) then
                NewLine(lineEnding)

            elementFactory.CreateOpenStatement(ns)
            NewLine(lineEnding)
            if indent > 0 then
                Whitespace(indent)

            // Add space after new opens group.
            if not (moduleMember :? IOpenStatement) && not (isFollowedByEmptyLineOrComment moduleMember) then
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
            if not (anchor :? IOpenStatement) && not (isFollowedByEmptyLineOrComment anchor) then
                NewLine(lineEnding)
        ] |> ignore

    let rec addOpenToOpensGroup (opens: IOpenStatement list) =
        match opens with
        | [] -> failwith "Expecting non-empty list"
        | openStatement :: rest ->

        if canInsertBefore openStatement ns then
            insertBeforeModuleMember openStatement
        else
            match rest with
            | [] -> insertAfterAnchor openStatement openStatement.Indent
            | _ -> addOpenToOpensGroup rest

    let moduleDecl = findModuleToInsert fsFile offset settings
    match tryGetFirstOpensGroup moduleDecl with
    | Some opens -> addOpenToOpensGroup opens
    | _ ->

    match moduleDecl with
    | :? IAnonModuleDeclaration when (fsFile.GetPsiModule() :? SandboxPsiModule) ->
        moduleDecl.MembersEnumerable
        |> Seq.skipWhile (fun m -> not (m :? IDoStatement))
        |> Seq.tail
        |> Seq.tryHead
        |> Option.iter insertBeforeModuleMember
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

let isInOpen (referenceName: IReferenceName) =
    match skipIntermediateParentsOfSameType referenceName with
    | null -> false
    | node -> node.Parent :? IOpenStatement


[<RequireQualifiedAccess>]
type OpenScope =
    | Global
    | Range of range: TreeTextRange

[<RequireQualifiedAccess>]
module OpenScope =
    let includesOffset (offset: TreeOffset) (scope: OpenScope) =
        match scope with
        | OpenScope.Range range -> range.Contains(offset)
        | _ -> true

    let inAnyScope (treeNode: ITreeNode) (scopes: IList<OpenScope>) =
        if scopes.Count = 0 then false else

        let offset = treeNode.GetTreeStartOffset()
        if scopes.Count = 1 then
            includesOffset offset scopes.[0]
        else
            scopes |> Seq.exists (includesOffset offset)

type OpenedModulesProvider(fsFile: IFSharpFile) =
    let map = OneToListMap<string, OpenScope>()

    let document = fsFile.GetSourceFile().Document
    let psiModule = fsFile.GetPsiModule()
    let symbolScope = getModuleOnlySymbolScope psiModule

//    let getQualifiedName (element: IClrDeclaredElement) =
//        match toQualifiedList element with
//        | [] -> "global"
//        | names -> names |> List.map (fun el -> el.GetSourceName()) |> String.concat "."

    let import scope (element: IClrDeclaredElement) =
        map.Add(element.GetSourceName(), scope)
        for autoImportedModule in getNestedAutoImportedModules element symbolScope do
            map.Add(autoImportedModule.GetSourceName(), scope)

    do
        import OpenScope.Global symbolScope.GlobalNamespace

        for moduleDecl in fsFile.ModuleDeclarationsEnumerable do
            let topLevelModuleDecl = moduleDecl.As<ITopLevelModuleLikeDeclaration>()
            if isNotNull topLevelModuleDecl then
                // todo: use inner range only
                let scope = OpenScope.Range(topLevelModuleDecl.GetTreeTextRange())
                match topLevelModuleDecl.DeclaredElement with
                | :? INamespace as ns -> import scope ns
                | :? ITypeElement as ty -> import scope (ty.GetContainingNamespace())
                | _ -> ()

        match fsFile.GetParseAndCheckResults(true, "OpenedModulesProvider") with
        | None -> ()
        | Some results ->

        for fcsOpenDecl in results.CheckResults.OpenDeclarations do
            let scope = OpenScope.Range(getTreeTextRange document fcsOpenDecl.AppliedScope)
            for fcsEntity in fcsOpenDecl.Modules do
                let declaredElement = fcsEntity.GetDeclaredElement(psiModule).As<IClrDeclaredElement>()
                if isNotNull declaredElement then
                    import scope declaredElement

    member x.OpenedModuleScopes = map

let openedModulesProvider = Key<OpenedModulesProvider>("OpenedModulesProvider")
