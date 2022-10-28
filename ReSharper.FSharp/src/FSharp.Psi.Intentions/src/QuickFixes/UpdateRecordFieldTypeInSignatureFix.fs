namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.QuickFixes.SignatureFixUtil

type UpdateRecordFieldTypeInSignatureFix(error: FieldNotContainedTypesDifferError) =
    inherit FSharpQuickFixBase()
    
    let recordFieldDeclaration =
        let recordRepresentation = RecordRepresentationNavigator.GetByFieldDeclaration(error.RecordFieldDeclaration)
        if isNull recordRepresentation then None else
        Some recordRepresentation


    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(error.RecordFieldDeclaration.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        match recordFieldDeclaration with
        | None -> ()
        | Some implementationRecordRepr ->
        let implFieldDecl = error.RecordFieldDeclaration
        let signatureRecordRepr = getSignatureRecordRepr implementationRecordRepr
        
        match signatureRecordRepr with
        | None -> ()
        | Some signatureRecordRepr ->

        if implementationRecordRepr.FieldDeclarations.Count <> signatureRecordRepr.FieldDeclarations.Count then
            updateSignatureFieldDecls implementationRecordRepr signatureRecordRepr
        else

        let signatureFieldDecl =
            signatureRecordRepr.FieldDeclarations
            |> Seq.tryPick (fun fd -> if fd.SourceName = implFieldDecl.SourceName then Some fd else None)

        Option.iter (updateSignatureFieldDecl implFieldDecl) signatureFieldDecl

    override this.IsAvailable _ =
        isValid error.RecordFieldDeclaration && Option.isSome recordFieldDeclaration

    override this.Text = "Update field type in signature file."
