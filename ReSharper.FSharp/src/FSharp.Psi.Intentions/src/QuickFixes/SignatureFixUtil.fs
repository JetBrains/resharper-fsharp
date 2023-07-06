module JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.QuickFixes.SignatureFixUtil

open System
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Psi.Tree

module Option =
    let both o1 o2 =
        match o1, o2 with
        | Some o1, Some o2 -> Some (o1, o2)
        | _ -> None

let getRecordRepresentation (typeDecl: IFSharpTypeDeclaration) =
    match typeDecl.TypeRepresentation with
    | :? IRecordRepresentation as rr -> Some rr
    | _ -> None

let getSignatureRecordRepr (implementationRecordRepr: IRecordRepresentation) =
    let decl = implementationRecordRepr.TypeDeclaration.DeclaredElement
    let declarations = if isNull decl then Seq.empty else decl.GetDeclarations()

    declarations
    |> Seq.tryPick (fun d ->
        match d with
        | :? IFSharpTypeDeclaration as signatureTypeDecl when
            signatureTypeDecl.GetSourceFile().IsFSharpSignatureFile ->
            getRecordRepresentation signatureTypeDecl
        | _ -> None)

let getFieldType (rfd:IRecordFieldDeclaration) =
    if isNull rfd then None else
    let symbolUse = rfd.GetFcsSymbolUse()
    if isNull symbolUse then None else
    match symbolUse.Symbol with
    | :? FSharpField as ff -> Some ff.FieldType
    | _ -> None

let getDisplayPlayContext (rfd:IRecordFieldDeclaration) =
    if isNull rfd then None else
    let symbolUse = rfd.GetFcsSymbolUse()
    if isNull symbolUse then None else
    Some symbolUse.DisplayContext

let mkRecordFieldDeclaration isMutable (implFieldDecl: IRecordFieldDeclaration) (implementationFieldType: FSharpType) displayContext =
    let factory = implFieldDecl.CreateElementFactory()
    let typeUsage = factory.CreateTypeUsage(implementationFieldType.Format displayContext, TypeUsageContext.TopLevel)
    factory.CreateRecordFieldDeclaration(isMutable, implFieldDecl.DeclaredName, typeUsage)

let updateSignatureFieldDecl (implFieldDecl: IRecordFieldDeclaration) (signatureFieldDecl: IRecordFieldDeclaration) =
    let signatureFieldType = getFieldType signatureFieldDecl
    let displayContext = getDisplayPlayContext signatureFieldDecl
    let implementationFieldType =  getFieldType implFieldDecl

    let fieldTypeAreEqual =
        match implementationFieldType, signatureFieldType with
        | Some i, Some s -> i = s
        | _ -> false

    let mutableAreEqual = implFieldDecl.IsMutable = signatureFieldDecl.IsMutable
    let namesAreEqual = implFieldDecl.SourceName = signatureFieldDecl.SourceName
    
    if not mutableAreEqual then
        signatureFieldDecl.SetIsMutable(implFieldDecl.IsMutable)

    if not namesAreEqual then
        signatureFieldDecl.SetName(implFieldDecl.NameIdentifier.Name, ChangeNameKind.SourceName)

    if not fieldTypeAreEqual then
        match Option.both implementationFieldType displayContext with
        | None -> ()
        | Some (t, d) ->
        let factory = implFieldDecl.CreateElementFactory()
        let updatedTypeUsage = factory.CreateTypeUsage(t.Format d, TypeUsageContext.TopLevel)
        ModificationUtil.ReplaceChild(signatureFieldDecl.TypeUsage, updatedTypeUsage)
        |> ignore

let updateSignatureFieldDecls (implementationRecordRepr: IRecordRepresentation) (signatureRecordRepr: IRecordRepresentation) =
    let signatureFieldCount = signatureRecordRepr.FieldDeclarations.Count

    implementationRecordRepr.FieldDeclarations
    |> Seq.iter (fun implFieldDecl ->
        let index = implementationRecordRepr.FieldDeclarations.IndexOf(implFieldDecl)
        
        if index < signatureFieldCount then
            // The signature record definition has a field at the current index
            // The name or type might be wrong
            let signatureFieldDecl = signatureRecordRepr.FieldDeclarations[index]
            updateSignatureFieldDecl implFieldDecl signatureFieldDecl
        else
        // The signature record definition is out of fields.
        // New ones from the implementation should be added.
        let implementationFieldType = getFieldType implFieldDecl
        let displayContext =
            signatureRecordRepr.FieldDeclarations
            |> Seq.tryHead
            |> Option.bind getDisplayPlayContext

        match Option.both implementationFieldType displayContext with
        | None -> ()
        | Some (implementationFieldType, displayContext) ->

        let recordFieldBinding =
            mkRecordFieldDeclaration
                (isNotNull implFieldDecl.MutableKeyword)
                implFieldDecl
                implementationFieldType
                displayContext

        let lastSignatureFieldDecl = signatureRecordRepr.FieldDeclarations.Last() :> ITreeNode
        let newlineNode = NewLine(lastSignatureFieldDecl.GetLineEnding()) :> ITreeNode
        let spaces =
            let startPos = lastSignatureFieldDecl.GetDocumentStartOffset().ToDocumentCoords()
            Whitespace(Convert.ToInt32(startPos.Column))

        addNodesAfter lastSignatureFieldDecl [| newlineNode; spaces; recordFieldBinding |]
        |> ignore
    )

    if signatureFieldCount > implementationRecordRepr.FieldDeclarations.Count then
        [ implementationRecordRepr.FieldDeclarations.Count .. (signatureFieldCount - 1) ]
        |> List.iter (fun idx -> signatureRecordRepr.FieldDeclarations.Item idx |> deleteChild)

type BindingPair =
    | BindingPair of
        implBinding: IBindingLikeDeclaration *
        implMember: IFSharpMember *
        sigBinding: IBindingLikeDeclaration *
        sigMember: IFSharpMember

let tryFindBindingPairFromTopReferencePat (implTopRefPat: ITopReferencePat) =
    match implTopRefPat.Binding, implTopRefPat.DeclaredElement.As<IFSharpMember>() with
    | null, _ | _, null -> None
    | implBinding, implMember ->

    let tryPickFromDeclaration (condition: IReferencePat -> bool) (declaration: IDeclaration) =
        match declaration with
        | :? IReferencePat as pat when pat.IsFSharpSigFile() && condition pat ->
            Option.both (Option.ofObj pat.Binding) (Option.ofObj (pat.DeclaredElement.As<IFSharpMember>()))
            |> Option.map (fun (sigBinding, sigMember) -> BindingPair(implBinding, implMember, sigBinding, sigMember))
        | _ -> None
    
    implMember.GetDeclarations()
    |> Seq.tryPick (tryPickFromDeclaration (fun _ -> true))
    |> Option.orElseWith (fun () ->
        let parentDeclarations = implMember.ContainingType.GetDeclarations()

        // Find the parent signature counter part
        let parentSignatureDeclaration =
            parentDeclarations
            |> Seq.tryPick (fun d ->
                match d with
                | :? INamedModuleDeclaration as signatureModule when signatureModule.IsFSharpSigFile() ->
                    Some signatureModule
                | _ -> None)

        parentSignatureDeclaration
        |> Option.bind (fun signatureModule ->
            signatureModule.MemberDeclarations
            |> Seq.tryPick (
                tryPickFromDeclaration (fun (sigRefPat: IReferencePat) ->
                    match implBinding.HeadPattern with
                    | :? IReferencePat as implPat ->
                        implPat.DeclaredName = sigRefPat.DeclaredName
                        || implPat.Identifier.Name = sigRefPat.Identifier.Name
                    | _ -> false)
            )
        )
    )