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

let toQualifiedList (fsFile: IFSharpFile) (declaredElement: IClrDeclaredElement) =
    let rec loop acc (declaredElement: IClrDeclaredElement) =
        match declaredElement with
        | :? INamespace as ns ->
            if ns.IsRootNamespace then acc else
            loop (declaredElement :: acc) (ns.GetContainingNamespace())

        | :? ITypeElement as typeElement ->
            let acc = declaredElement :: acc

            match typeElement.GetContainingType() with
            | null -> loop acc (typeElement.GetContainingNamespace())

            | :? IFSharpModule as fsModule when
                    fsModule.IsAnonymous &&
                    let decls = fsFile.ModuleDeclarations
                    decls.Count = 1 && decls.[0].DeclaredElement == fsModule ->
                acc

            | containingType -> loop acc containingType

        | _ -> failwithf "Expecting namespace to type element"

    loop [] declaredElement


[<RequireQualifiedAccess>]
type ModuleToImport =
    | DeclaredElement of element: IClrDeclaredElement

    /// FCS-provided namespace to import that uses the previous import logic,
    /// will be removed when remaining cases are moved to use declared elements.
    | FullName of string

    member this.GetQualifiedElementList(moduleDeclaration: IModuleLikeDeclaration) =
        match this with
        | FullName _ -> []
        | DeclaredElement(declaredElement) ->

        let elements = toQualifiedList moduleDeclaration.FSharpFile declaredElement
        if not (moduleDeclaration :? IModuleDeclaration) then elements else

        match List.skipWhile ((!=) moduleDeclaration.DeclaredElement) elements with
        | [] -> elements
        | _ :: elements -> elements

    member this.GetNamespace(moduleDeclaration: IModuleLikeDeclaration) =
        match this with
        | FullName(ns) -> ns
        | DeclaredElement _ ->

        let namingService = NamingManager.GetNamingLanguageService(moduleDeclaration.Language)

        let elements = this.GetQualifiedElementList(moduleDeclaration)
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

let tryGetCommonParentModuleDecl (context: ITreeNode) fsFile (moduleToImport: ModuleToImport) =
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

        let importModuleQualifiedList = toQualifiedList fsFile declaredElement
        let topLevelDecl = List.head moduleDecls

        let commonPrefixImportModuleList =
            importModuleQualifiedList |> List.skipWhile (fun d -> d.Equals(topLevelDecl.DeclaredElement) |> not)        

        if commonPrefixImportModuleList.IsEmpty then null else

        (commonPrefixImportModuleList, moduleDecls)
        ||> Seq.zip
        |> Seq.takeWhile (fun (element, decl) -> element.Equals(decl.DeclaredElement))
        |> Seq.last
        |> snd

    | _ -> null

let findModuleToInsert (fsFile: IFSharpFile) (offset: DocumentOffset) (settings: IContextBoundSettingsStore)
        (moduleToImport: ModuleToImport): IModuleLikeDeclaration * bool =

    let containingModuleDecl = fsFile.GetNode<IModuleLikeDeclaration>(offset)
    let commonParentDecl = tryGetCommonParentModuleDecl containingModuleDecl fsFile moduleToImport
    if isNotNull commonParentDecl then commonParentDecl, true else

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

let addOpen (offset: DocumentOffset) (fsFile: IFSharpFile) settings (moduleToImport: ModuleToImport) =
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

    let moduleDecl, checkMembers = findModuleToInsert fsFile offset settings moduleToImport
    let ns = moduleToImport.GetNamespace(moduleDecl)
    if ns.IsEmpty() then () else

    let qualifiedElementList = moduleToImport.GetQualifiedElementList(moduleDecl)
    let firstModule = qualifiedElementList |> List.tryHead |> Option.toObj

    let moduleMembers: IModuleMember list =
        let moduleMembers =
            moduleDecl.MembersEnumerable
            |> List.ofSeq
            |> List.skipWhile (fun moduleMember -> moduleMember :? IHashDirective)

        if isNull firstModule || not checkMembers then moduleMembers else

        let skippedMembers = 
            moduleMembers
            |> List.skipWhile (function
                | :? IModuleLikeDeclaration as decl -> firstModule.Equals(decl.DeclaredElement) |> not
                | _ -> true)
            |> List.tail

        if Seq.isEmpty skippedMembers then moduleMembers else skippedMembers

    match tryGetFirstOpensGroup moduleMembers with
    | Some opens ->
        // note: partial name modules may be added without being a duplicate (we don't add them now, though)
        if Seq.exists (duplicates ns) opens |> not then
            addOpenToOpensGroup opens ns
    | _ ->

    match moduleDecl with
    | :? IAnonModuleDeclaration when (fsFile.GetPsiModule() :? SandboxPsiModule) ->
        moduleMembers
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

    match Seq.tryHead moduleMembers with
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

    let qualifiedModuleToOpen = toQualifiedList fsFile moduleToOpen
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
