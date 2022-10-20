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
    let typeUsage = factory.CreateTypeUsage(implementationFieldType.Format displayContext)
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
        let updatedTypeUsage = factory.CreateTypeUsage(t.Format d)
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
