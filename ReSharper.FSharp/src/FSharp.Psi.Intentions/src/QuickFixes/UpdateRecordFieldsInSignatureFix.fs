namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.PsiUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type UpdateRecordFieldsInSignatureFix(error: DefinitionsInSigAndImplNotCompatibleFieldWasPresentError) =
    inherit FSharpQuickFixBase()

    let getRecordRepresentation (typeDecl: IFSharpTypeDeclaration) =
        match typeDecl.TypeRepresentation with
        | :? IRecordRepresentation as rr -> Some rr
        | _ -> None

    let implementationRecordRepr =
        let typeDecl = FSharpTypeDeclarationNavigator.GetByIdentifier(error.TypeName)
        getRecordRepresentation typeDecl

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(error.TypeName.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        match implementationRecordRepr with
        | None -> ()
        | Some implementationRecordRepr ->
            let decl = implementationRecordRepr.TypeDeclaration.DeclaredElement
            let declarations = if isNull decl then Seq.empty else decl.GetDeclarations()

            let signatureRecordRepr =
                declarations
                |> Seq.tryPick (fun d ->
                    match d with
                    | :? IFSharpTypeDeclaration as signatureTypeDecl when
                        signatureTypeDecl.GetSourceFile().IsFSharpSignatureFile ->
                        getRecordRepresentation signatureTypeDecl
                    | _ -> None)

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
                        if implFieldDecl.SourceName = signatureFieldDecl.SourceName &&
                           implFieldDecl.DeclaredElement.ContainingType = signatureFieldDecl.DeclaredElement.ContainingType then
                            // fields are identical
                            ()
                        elif implFieldDecl.SourceName <> signatureFieldDecl.SourceName then
                            // field names are different, update signature field name
                            signatureFieldDecl.SetName(implFieldDecl.NameIdentifier.Name)
                        else
                             // Update entire field, I'm not sure how to update the type only.
                             ModificationUtil.ReplaceChild(signatureFieldDecl :> IFSharpTreeNode, implFieldDecl)
                             |> ignore
                    else
                        // The signature record definition is out of fields.
                        // New ones from the implementation should be added.
                        let lastSignatureFieldDecl = signatureRecordRepr.FieldDeclarations.Last() :> ITreeNode
                        let newlineNode = NewLine(lastSignatureFieldDecl.GetLineEnding()) :> ITreeNode
                        ModificationUtil.AddChildAfter(lastSignatureFieldDecl, newlineNode)
                        |> fun node ->
                            let startPos = lastSignatureFieldDecl.GetDocumentStartOffset().ToDocumentCoords()
                            let spaces = Whitespace(Convert.ToInt32(startPos.Column))
                            ModificationUtil.AddChildAfter(node, spaces)
                        |> fun node ->
                            ModificationUtil.AddChildAfter(node, implFieldDecl)
                            |> ignore
                )

    override this.IsAvailable _ =
        isValid error.TypeName && Option.isSome implementationRecordRepr

    override this.Text = "Update record fields in signature file."
