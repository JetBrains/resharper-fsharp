namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.QuickFixes.SignatureFixUtil

type UpdateRecordFieldsInSignatureFix(error: DefinitionsInSigAndImplNotCompatibleFieldWasPresentError) =
    inherit FSharpQuickFixBase()

    let implementationRecordRepr =
        let typeDecl = FSharpTypeDeclarationNavigator.GetByIdentifier(error.TypeName)
        getRecordRepresentation typeDecl
    
    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(error.TypeName.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        match implementationRecordRepr with
        | None -> ()
        | Some implementationRecordRepr ->

        let signatureRecordRepr = getSignatureRecordRepr implementationRecordRepr

        match signatureRecordRepr with
        | None -> ()
        | Some signatureRecordRepr ->

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

    override this.IsAvailable _ =
        isValid error.TypeName && Option.isSome implementationRecordRepr

    override this.Text = "Update record fields in signature file."
