[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Util.OpensUtil

open System.Collections.Generic
open JetBrains.Application.Settings
open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Files.SandboxFiles
open JetBrains.ReSharper.Psi.Naming
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
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

        | _ -> failwithf "Expecting a namespace or a type element"

    loop [] declaredElement


[<RequireQualifiedAccess>]
type ModuleToImport =
    | DeclaredElement of element: IClrDeclaredElement

    /// FCS-provided namespace to import that uses the previous import logic,
    /// will be removed when remaining cases are moved to use declared elements.
    | FullName of string

    member this.GetQualifiedElementList(moduleDeclaration: IModuleLikeDeclaration, skipNamespaces) =
        match this with
        | FullName _ -> []
        | DeclaredElement(element) ->

        let declElement = moduleDeclaration.DeclaredElement
        if (element :? INamespace && element.Equals(declElement)) then [] else

        let elements = toQualifiedList element
        if not skipNamespaces && not (moduleDeclaration :? IModuleDeclaration) then elements else

        // When importing `Ns.Module.NestedModule` inside `Ns.Module`, skip the parent part.
        match List.skipWhile (declElement.Equals >> not) elements with
        | [] ->
            match declElement with
            | :? IFSharpModule as fsModule when element.Equals(fsModule.GetContainingNamespace()) -> []
            | _ -> elements

        | _ :: skippedElements ->

        // Don't insert `open Ns2.Module` for `Ns1.Ns2.Module`, prefer the full name.
        match skippedElements with
        | :? INamespace :: _ -> elements
        | _ -> skippedElements

    member this.GetNamespace(moduleDeclaration: IModuleLikeDeclaration, insertFullNamespace) =
        match this with
        | FullName(ns) -> ns
        | DeclaredElement element ->

        let namingService = NamingManager.GetNamingLanguageService(moduleDeclaration.Language)

        let elements = this.GetQualifiedElementList(moduleDeclaration, not insertFullNamespace && element :? ITypeElement)
        elements
        |> List.map (fun el ->
            let sourceName = el.GetSourceName()
            namingService.MangleNameIfNecessary(sourceName))
        |> String.concat "."


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

let tryGetCommonParentModuleDecl (context: ITreeNode) (moduleToImport: ModuleToImport) =
    match moduleToImport with
    | ModuleToImport.DeclaredElement(declaredElement) ->
        let moduleDecls = 
            context.ContainingNodes<IModuleLikeDeclaration>(true).ToEnumerable()
            |> Seq.toList
            |> List.rev

        let moduleDecls = 
            match moduleDecls with
            | :? IGlobalNamespaceDeclaration :: decls -> decls
            | _ -> moduleDecls

        if moduleDecls.IsEmpty then None else

        let importModuleQualifiedList = toQualifiedList declaredElement
        let topLevelDecl = List.head moduleDecls

        let commonPrefixImportModuleList =
            importModuleQualifiedList |> List.skipWhile (fun d -> d.Equals(topLevelDecl.DeclaredElement) |> not)        

        if commonPrefixImportModuleList.IsEmpty then None else

        let matchingDecls, restDecls = 
            (commonPrefixImportModuleList, moduleDecls)
            ||> Seq.zip
            |> List.ofSeq
            |> List.partition (fun (element, decl) -> element.Equals(decl.DeclaredElement))

        let decl = matchingDecls |> List.last |> snd
        Some(decl, not restDecls.IsEmpty || commonPrefixImportModuleList.Length > moduleDecls.Length)

    | _ -> None

let findModuleToInsertTo (fsFile: IFSharpFile) (offset: DocumentOffset) (settings: IContextBoundSettingsStore)
        (moduleToImport: ModuleToImport): IModuleLikeDeclaration * bool =

    let containingModuleDecl = fsFile.GetNode<IModuleLikeDeclaration>(offset)
    match tryGetCommonParentModuleDecl containingModuleDecl moduleToImport with
    | Some(decl, searchAnchor) -> decl, searchAnchor
    | _ ->

    if not (settings.GetValue(fun key -> key.TopLevelOpenCompletion)) then
        match fsFile.GetNode<IModuleLikeDeclaration>(offset) with
        | :? IDeclaredModuleLikeDeclaration as moduleDecl when
                // todo: F# 6: attributes can be after `module` keyword
                let keyword = moduleDecl.ModuleOrNamespaceKeyword
                isNotNull keyword && keyword.GetTreeStartOffset().Offset < offset.Offset ->
            moduleDecl :> IModuleLikeDeclaration, false
        | moduleDecl ->
            moduleDecl.GetContainingNode<IModuleLikeDeclaration>(), false
    else
        match fsFile.GetNode<ITopLevelModuleLikeDeclaration>(offset) with
        | null -> fsFile.GetNode<IAnonModuleDeclaration>(offset) :> _, false
        | moduleDecl -> moduleDecl :> _, false

let tryGetFirstOpensGroup (members: IModuleMember list) =
    let opens =
        members
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
    use writeLock = WriteLockCookie.Create(true)
    use disableFormatter = new DisableCodeFormatter()

    removeModuleMember openStatement

let isSystemNs ns =
    ns = "System" || startsWith "System." ns

let canInsertBefore (openStatement: IOpenStatement) ns =
    if isSystemNs ns then
        not openStatement.IsSystem ||
        ns < openStatement.ReferenceName.QualifiedName
    else
        not openStatement.IsSystem &&
        ns < openStatement.ReferenceName.QualifiedName

let addOpen (offset: DocumentOffset) (fsFile: IFSharpFile) (settings: IContextBoundSettingsStore) (moduleToImport: ModuleToImport) =
    let elementFactory = fsFile.CreateElementFactory()
    let lineEnding = fsFile.GetLineEnding()

    let insertBeforeModuleMember (ns: string) (moduleMember: IModuleMember) =
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
            if not (moduleMember :? IOpenStatement) then
                NewLine(lineEnding)
                if indent > 0 then
                    Whitespace(indent)
        ] |> ignore

    let insertAfterAnchor (ns: string) (anchor: ITreeNode) indent =
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

    let duplicates (ns: string) (openStatement: IOpenStatement) =
        let referenceName = openStatement.ReferenceName
        isNotNull referenceName && referenceName.QualifiedName = ns

    let rec addOpenToOpensGroup (opens: IOpenStatement list) (ns: string) =
        match opens with
        | [] -> failwith "Expecting non-empty list"
        | openStatement :: rest ->

        if canInsertBefore openStatement ns then
            insertBeforeModuleMember ns openStatement
        else
            match rest with
            | [] -> insertAfterAnchor ns openStatement openStatement.Indent
            | _ -> addOpenToOpensGroup rest ns

    let moduleDecl, searchAnchor = findModuleToInsertTo fsFile offset settings moduleToImport
    if isNull moduleDecl then () else // can happen when the node is not in a proper tree, i.e. during generation

    let qualifiedElementList = moduleToImport.GetQualifiedElementList(moduleDecl, true)
    let firstModule = qualifiedElementList |> List.tryHead |> Option.toObj

    // When the anchor module decl is not found among members, it's likely in a different namespace part.
    // Prefer full namespace when importing a module external to current namespace declaration.
    let inScopeModuleMembers, insertFullNamespace =
        let moduleMembers =
            // Workaround for script files with references on top.
            // todo: check actual scopes for referenced assemblies/packages/projects
            // todo: filter hash directives?
            moduleDecl.MembersEnumerable
            |> List.ofSeq
            |> List.skipWhile (fun moduleMember -> moduleMember :? IHashDirective)

        if isNull firstModule || not searchAnchor then moduleMembers, false else

        let skippedMembers = 
            let moduleMembers = 
                moduleMembers
                |> List.skipWhile (function
                    | :? IModuleLikeDeclaration as decl -> firstModule.Equals(decl.DeclaredElement) |> not
                    | _ -> true)

            if not moduleMembers.IsEmpty then moduleMembers.Tail else moduleMembers

        if skippedMembers.IsEmpty then moduleMembers, true else skippedMembers, false

    let ns = moduleToImport.GetNamespace(moduleDecl, insertFullNamespace)
    if ns.IsEmpty() then () else

    match tryGetFirstOpensGroup inScopeModuleMembers with
    | Some opens ->
        // note: partial name modules may be added without being a duplicate (we don't add them now, though)
        if Seq.exists (duplicates ns) opens |> not then
            addOpenToOpensGroup opens ns
    | _ ->

    match moduleDecl with
    | :? IAnonModuleDeclaration when (fsFile.GetPsiModule() :? SandboxPsiModule) ->
        inScopeModuleMembers
        |> Seq.skipWhile (fun m -> not (m :? IDoLikeStatement)) // todo: check this
        |> Seq.tail
        |> Seq.tryHead
        |> Option.iter (insertBeforeModuleMember ns)

    | :? IDeclaredModuleDeclaration as moduleDecl when
            let keyword = moduleDecl.ModuleOrNamespaceKeyword
            isNotNull keyword && keyword.GetTreeStartOffset().Offset > offset.Offset ->
        // Don't insert open after the reference.
        ()

    | _ ->

    match Seq.tryHead inScopeModuleMembers with
    | None -> failwith "Expecting any module member"
    | Some memberToInsertBefore ->

    let firstModuleMember = moduleDecl.MembersEnumerable |> Seq.tryHead |> Option.toObj
    if firstModuleMember != memberToInsertBefore || moduleDecl :? IAnonModuleDeclaration then
        insertBeforeModuleMember ns memberToInsertBefore else

    let indent = memberToInsertBefore.Indent

    let anchor =
        match moduleDecl with
        | :? INestedModuleDeclaration as nestedModule -> nestedModule.EqualsToken :> ITreeNode
        | :? ITopLevelModuleLikeDeclaration as moduleDecl -> moduleDecl.NameIdentifier :> _
        | _ -> failwithf "Unexpected module: %O" moduleDecl

    insertAfterAnchor ns anchor indent

let addOpens (reference: FSharpSymbolReference) (typeElement: ITypeElement) =
    let referenceOwner = reference.GetElement()
    use writeCookie = WriteLockCookie.Create(referenceOwner.IsPhysical())

    let moduleToOpen = getModuleToOpen typeElement
    let fsFile = referenceOwner.FSharpFile

    let qualifiedModuleToOpen = toQualifiedList moduleToOpen
    if qualifiedModuleToOpen.IsEmpty then reference else

    let moduleToImport = ModuleToImport.DeclaredElement(moduleToOpen)
    let settings = fsFile.GetSettingsStoreWithEditorConfig()
    addOpen (referenceOwner.GetDocumentStartOffset()) fsFile settings moduleToImport
    reference

let getContainingModules (treeNode: ITreeNode) =
    treeNode.ContainingNodes<IModuleLikeDeclaration>().ToEnumerable()
    |> Seq.map (fun decl -> decl.DeclaredElement)
    |> Seq.filter isNotNull
    |> HashSet

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
            includesOffset offset scopes[0]
        else
            scopes |> Seq.exists (includesOffset offset)

type OpenedModulesProvider(fsFile: IFSharpFile) =
    let map = OneToListMap<string, OpenScope>()

    let document = fsFile.GetSourceFile().Document
    let psiModule = fsFile.GetPsiModule()
    let symbolScope = getModuleOnlySymbolScope psiModule false

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
