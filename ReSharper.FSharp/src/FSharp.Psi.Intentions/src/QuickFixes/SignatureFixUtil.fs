module JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.QuickFixes.SignatureFixUtil

open System
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Psi.Tree

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
    match symbolUse.Symbol with
    | :? FSharpField as ff -> Some (ff.FieldType, symbolUse.DisplayContext)
    | _ -> None

let createSignatureTypeUsage (factory: IFSharpElementFactory) (t: FSharpType, d: FSharpDisplayContext) : ITypeUsage =
    factory.CreateTypeUsage(t.Format d)

let updateSignatureFieldDecl (implFieldDecl: IRecordFieldDeclaration) (signatureFieldDecl: IRecordFieldDeclaration) =
    let fieldTypeAreEqual =
        let signatureFieldType = getFieldType signatureFieldDecl
        let implementationFieldType =  getFieldType implFieldDecl
        match implementationFieldType, signatureFieldType with
        | None, None
        | Some _, None
        | None, Some _ -> false
        | Some (i, _), Some (s, _) -> i = s

    if implFieldDecl.SourceName = signatureFieldDecl.SourceName && fieldTypeAreEqual then
        // fields are identical
        ()
    elif implFieldDecl.SourceName <> signatureFieldDecl.SourceName then
        // field names are different, update signature field name
        signatureFieldDecl.SetName(implFieldDecl.NameIdentifier.Name, ChangeNameKind.CompiledName)
    else
        let implementationFieldType = getFieldType implFieldDecl
        match implementationFieldType with
        | None -> ()
        | Some tu ->

        let updatedTypeUsage = createSignatureTypeUsage (signatureFieldDecl.CreateElementFactory()) tu
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
            let factory = implFieldDecl.CreateElementFactory()
            let implementationFieldType = getFieldType implFieldDecl
            match implementationFieldType with
            | None -> ()
            | Some implementationFieldType ->

            let typeUsage = createSignatureTypeUsage factory implementationFieldType
            let recordFieldBinding = factory.CreateRecordFieldDeclaration(implFieldDecl.DeclaredName, typeUsage)
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
        