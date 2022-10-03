namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.QuickFixes.SignatureFixUtil

type UpdateRecordFieldsInSignatureFix(typeName: IFSharpIdentifier) =
    inherit FSharpQuickFixBase()

    let implementationRecordRepr =
        let typeDecl = FSharpTypeDeclarationNavigator.GetByIdentifier(typeName)
        getRecordRepresentation typeDecl
    
    new(error: DefinitionsInSigAndImplNotCompatibleFieldWasPresentError) =
        UpdateRecordFieldsInSignatureFix(error.TypeName)
    new(error: DefinitionsInSigAndImplNotCompatibleFieldOrderDifferError) =
        UpdateRecordFieldsInSignatureFix(error.TypeName)
    
    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(typeName.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        match implementationRecordRepr with
        | None -> ()
        | Some implementationRecordRepr ->

        let signatureRecordRepr = getSignatureRecordRepr implementationRecordRepr

        match signatureRecordRepr with
        | None -> ()
        | Some signatureRecordRepr ->
            
        updateSignatureFieldDecls implementationRecordRepr signatureRecordRepr

    override this.IsAvailable _ =
        isValid typeName && Option.isSome implementationRecordRepr

    override this.Text = "Update record fields in signature file."
