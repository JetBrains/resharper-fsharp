namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

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

[<RequireQualifiedAccess>]
type private ParameterNameFromPattern =
    | NoNameFound
    | SingleName of name: IFSharpIdentifier * attributes: string
    | TupleName of ParameterNameFromPattern list

[<ContextAction(Name = "AddFunctionToSignatureFile", Group = "F#", Description = "Add function to signature file")>]
type AddFunctionToSignatureFileAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let (|ValFromImpl|_|) (symbol:FSharpSymbol) =
        match symbol with
        | :? FSharpMemberOrFunctionOrValue as valSymbol ->
            valSymbol.SignatureLocation
            |> Option.bind (fun range -> if range.FileName.EndsWith(".fs") then Some valSymbol else None)
        | _ -> None

    let rec tryFindParameterName (isTopLevel: bool) (p: IFSharpPattern) : ParameterNameFromPattern =
        match p.IgnoreInnerParens() with
        | :? ITypedPat as tp -> tryFindParameterName isTopLevel tp.Pattern
        | :? ILocalReferencePat as rp -> ParameterNameFromPattern.SingleName (rp.Identifier, "")
        | :? IAttribPat as ap ->
            match tryFindParameterName isTopLevel ap.Pattern with
            | ParameterNameFromPattern.SingleName(name, _) ->
                let attributes = Seq.map (fun (al:IAttributeList) -> al.GetText()) ap.AttributeListsEnumerable |> String.concat ""
                ParameterNameFromPattern.SingleName(name, attributes)
            | _ ->
                ParameterNameFromPattern.NoNameFound
        | :? ITuplePat as tp ->
            if not isTopLevel then
                ParameterNameFromPattern.NoNameFound
            else

            Seq.map (tryFindParameterName false) tp.Patterns
            |> Seq.toList
            |> ParameterNameFromPattern.TupleName
        | _ -> ParameterNameFromPattern.NoNameFound

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
        if isNull symbolUse then None else
        
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
                let parameterAtIndex = tryFindParameterName true (binding.ParameterPatterns.Item(index))

                match t, parameterAtIndex with
                | :? IFunctionTypeUsage as ft, ParameterNameFromPattern.NoNameFound ->
                    visit (index + 1) ft.ReturnTypeUsage

                | :? IFunctionTypeUsage as ft, ParameterNameFromPattern.SingleName (name, attributes) ->
                    match ft.ArgumentTypeUsage with
                    | :? IParameterSignatureTypeUsage as pstu ->
                        factory.CreateParameterSignatureTypeUsage(attributes, name, pstu.TypeUsage)
                        |> replace ft.ArgumentTypeUsage 
                    | _ -> ()

                    visit (index + 1) ft.ReturnTypeUsage

                | :? IFunctionTypeUsage as ft, ParameterNameFromPattern.TupleName multipleParameterNames ->
                    match ft.ArgumentTypeUsage with
                    | :? ITupleTypeUsage as tt when tt.Items.Count = multipleParameterNames.Length ->
                        (multipleParameterNames, tt.Items)
                        ||> Seq.zip
                        |> Seq.iter (fun (p,t) ->
                            match t, p with
                            | :? IParameterSignatureTypeUsage as pstu,  ParameterNameFromPattern.SingleName (name, attributes) ->
                                factory.CreateParameterSignatureTypeUsage(attributes, name, pstu.TypeUsage) 
                                |> replace t
                            | _ -> ()
                        )
                    | _ -> visit (index + 1) ft.ReturnTypeUsage
                | _ ->
                    ()

        if not binding.ParameterPatterns.IsEmpty then
            visit 0 typeInfo

        let valSig = factory.CreateBindingSignature(refPat, typeInfo)
        let newlineNode = NewLine(signatureModuleOrNamespaceDecl.GetLineEnding()) :> ITreeNode
        addNodesAfter signatureModuleOrNamespaceDecl.LastChild [| newlineNode; valSig |] |> ignore

        null

    override this.Text = "Add function to signature file"
