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

let updateSignatureTypeSpread (implSpread: IDeclarationTypeSpread) (signatureSpread: IDeclarationTypeSpread) =
    let implTypeName = implSpread.TypeName
    let signatureTypeName = signatureSpread.TypeName

    if isNull implTypeName || isNull signatureTypeName then () else
    if implTypeName.GetText() = signatureTypeName.GetText() then () else

    ModificationUtil.ReplaceChild(signatureTypeName, implTypeName.Copy()) |> ignore

let private mkSignatureMemberDecl (signatureRecordRepr: IRecordRepresentation) (implMemberDecl: IRecordMemberDeclaration) =
    match implMemberDecl with
    | :? IDeclarationTypeSpread as implSpread -> Some(implSpread.Copy() :> ITreeNode)
    | :? IRecordFieldDeclaration as implFieldDecl ->
        let implementationFieldType = getFieldType implFieldDecl
        let displayContext =
            signatureRecordRepr.FieldDeclarations
            |> Seq.tryHead
            |> Option.bind getDisplayPlayContext
            |> Option.orElseWith (fun _ -> getDisplayPlayContext implFieldDecl)

        match Option.both implementationFieldType displayContext with
        | None -> None
        | Some (implementationFieldType, displayContext) ->

        let recordFieldBinding =
            mkRecordFieldDeclaration
                (isNotNull implFieldDecl.MutableKeyword)
                implFieldDecl
                implementationFieldType
                displayContext

        Some(recordFieldBinding :> ITreeNode)

    | _ -> None

let updateSignatureFieldDecls (implementationRecordRepr: IRecordRepresentation) (signatureRecordRepr: IRecordRepresentation) =
    let signatureMemberCount = signatureRecordRepr.MemberDeclarations.Count

    implementationRecordRepr.MemberDeclarations
    |> Seq.iteri (fun index implMemberDecl ->
        if index < signatureMemberCount then
            match implMemberDecl, signatureRecordRepr.MemberDeclarations[index] with
            | (:? IRecordFieldDeclaration as implFieldDecl), (:? IRecordFieldDeclaration as signatureFieldDecl) ->
                updateSignatureFieldDecl implFieldDecl signatureFieldDecl
            | (:? IDeclarationTypeSpread as implSpread), (:? IDeclarationTypeSpread as signatureSpread) ->
                updateSignatureTypeSpread implSpread signatureSpread
            | _ -> ()
        else
        match mkSignatureMemberDecl signatureRecordRepr implMemberDecl with
        | None -> ()
        | Some newMemberDecl ->

        let lastSignatureMemberDecl = signatureRecordRepr.MemberDeclarations.Last() :> ITreeNode
        let newlineNode = NewLine(lastSignatureMemberDecl.GetLineEnding()) :> ITreeNode
        let spaces =
            let startPos = lastSignatureMemberDecl.GetDocumentStartOffset().ToDocumentCoords()
            Whitespace(Convert.ToInt32(startPos.Column))

        addNodesAfter lastSignatureMemberDecl [| newlineNode; spaces; newMemberDecl |]
        |> ignore
    )

    if signatureMemberCount > implementationRecordRepr.MemberDeclarations.Count then
        [ implementationRecordRepr.MemberDeclarations.Count .. (signatureMemberCount - 1) ]
        |> List.iter (fun idx -> signatureRecordRepr.MemberDeclarations.Item idx |> deleteChild)
