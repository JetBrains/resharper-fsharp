namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions

open FSharp.Compiler.Text
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Psi.Tree

[<ContextAction(Name = "AddFunctionToSignatureFile", Group = "F#", Description = "Add function to signature file")>]
type AddFunctionToSignatureFileAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let (|ValFromImpl|_|) (symbol:FSharpSymbol) =
        match symbol with
        | :? FSharpMemberOrFunctionOrValue as valSymbol ->
            valSymbol.SignatureLocation
            |> Option.bind (fun range -> if range.FileName.EndsWith(".fs") then Some valSymbol else None)
        | _ -> None

    let rec tryFindParameterName (p: IFSharpPattern) =
        match p.IgnoreInnerParens() with
        | :? ITypedPat as tp -> tryFindParameterName tp.Pattern
        | :? ILocalReferencePat as rp -> Some rp.Identifier
        | _ -> None

    let implBindingAndDecl =
        let currentFSharpFile = dataProvider.PsiFile
        if isNull currentFSharpFile then None else
        // Don't show context action in signature file.
        if currentFSharpFile.IsFSharpSigFile() then None else

        let fcsService = currentFSharpFile.FcsCheckerService
        if isNull fcsService || isNull fcsService.FcsProjectProvider then None else

        let hasSignature = fcsService.FcsProjectProvider.HasPairFile dataProvider.SourceFile
        if not hasSignature then None else

        let letBindings = dataProvider.GetSelectedElement<ILetBindingsDeclaration>()
        if isNull letBindings then None else
        // Currently excluding recursive bindings
        if letBindings.Bindings.Count <> 1 then None else
        let binding = letBindings.Bindings |> Seq.exactlyOne
        let refPat = binding.HeadPattern.As<IReferencePat>()
        if isNull refPat || isNull refPat.Reference then None else

        let moduleOrNamespaceDecl = QualifiableModuleLikeDeclarationNavigator.GetByMember(letBindings)
        if isNull moduleOrNamespaceDecl then None else
        let moduleOrNamespaceDeclaredElement = moduleOrNamespaceDecl.DeclaredElement
        if isNull moduleOrNamespaceDeclaredElement then None else

        let signatureCounterPart =
            moduleOrNamespaceDeclaredElement.GetDeclarations()
            |> Seq.tryPick (fun d -> if d.IsFSharpSigFile() then Some d else None)

        match signatureCounterPart with
        | None -> None
        | Some signatureCounterPart ->

        let symbolUse = refPat.GetFcsSymbolUse()
        match symbolUse.Symbol with
        | ValFromImpl valSymbol ->
            let text =
                valSymbol.FormatLayout(symbolUse.DisplayContext)
                |> Array.choose (fun (t : TaggedText) ->
                    match t.Tag with
                    | TextTag.UnknownEntity -> None
                    | _ -> Some t.Text)
                |> String.concat ""

            Some (refPat, binding, text, signatureCounterPart)
        | _ -> None

    override this.IsAvailable _ = Option.isSome implBindingAndDecl

    override this.ExecutePsiTransaction(_solution, _progress) =
        match implBindingAndDecl with
        | None -> null
        | Some (refPat, binding, text, signatureModuleOrNamespaceDecl) ->

        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
    
        let factory = signatureModuleOrNamespaceDecl.CreateElementFactory()
        let typeInfo = factory.CreateTypeUsageForSignature(text)

        // Enrich the type info with the found parameters from binding.
        let rec visit (index:int) (t: ITypeUsage) =
            if index = binding.ParameterPatterns.Count then
                match t with
                | :? IFunctionTypeUsage ->
                    // If the return type is a function itself, the safest thing to do is to wrap it in parentheses.
                    // Example: `let g _ = (*) 3`
                    // `val g: 'a -> int -> int` is not valid, `val g: 'a -> (int -> int)` is.
                    replace t (factory.WrapParenAroundTypeUsageForSignature(t))
                | _ -> ()
            else
                // TODO: take tuples into account.
                let parameterAtIndex = tryFindParameterName (binding.ParameterPatterns.Item(index))

                match t, parameterAtIndex with
                | :? IFunctionTypeUsage as ft, Some parameterName ->
                    match ft.ArgumentTypeUsage with
                    | :? IParameterSignatureTypeUsage as pstu ->
                        // Update the parameter name if it was found in the implementation file
                        // calling SetIdentifier on pstu does not add a ':' token.
                        let namedTypeUsage = factory.CreateParameterSignatureTypeUsage(parameterName, pstu.TypeUsage)
                        replace ft.ArgumentTypeUsage namedTypeUsage
                    | _ -> ()

                    visit (index + 1) ft.ReturnTypeUsage
                | :? IFunctionTypeUsage as ft, None ->
                    visit (index + 1) ft.ReturnTypeUsage
                | _ ->
                    ()

        if not binding.ParameterPatterns.IsEmpty then
            visit 0 typeInfo

        let valSig = factory.CreateBindingSignature(refPat, typeInfo)
        let newlineNode = NewLine(signatureModuleOrNamespaceDecl.GetLineEnding()) :> ITreeNode
        addNodesAfter signatureModuleOrNamespaceDecl.LastChild [| newlineNode; valSig; newlineNode |] |> ignore

        null

    override this.Text = "Add function to signature file"
