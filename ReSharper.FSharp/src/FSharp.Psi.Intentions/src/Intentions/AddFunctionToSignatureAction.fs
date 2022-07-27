namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open System
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl

[<ContextAction(Name = "AddFunctionToSignatureFile", Group = "F#", Description = "Add function to signature file")>]
type AddFunctionToSignatureFileAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    let getSignatureLocation () =
        let letBindings = dataProvider.GetSelectedElement<ILetBindings>()
        if isNull letBindings then None else

        let bindings = letBindings.Bindings
        if bindings.Count <> 1 then None else
        
        match bindings.First().HeadPattern.As<IReferencePat>() with
        | null -> None
        | pat -> pat.GetFcsSymbol().SignatureLocation

    let (|DeclaredNameInPattern|_|) (pat:IFSharpPattern) =
        match pat with
        | :? ILocalReferencePat as pat -> Some pat.DeclaredName
        | :? ITypedPat as tp ->
            match tp.Pattern.IgnoreInnerParens() with
            | :? ILocalReferencePat as pat -> Some pat.DeclaredName
            | _ -> None
        | _ -> None
    
    override x.Text = "Add to signature file"
    override this.IsAvailable _ =
        let currentFSharpFile = dataProvider.PsiFile
        let fcsService = currentFSharpFile.FcsCheckerService
        let hasSignature = fcsService.FcsProjectProvider.HasPairFile dataProvider.SourceFile
        if not hasSignature then false else
        
        match getSignatureLocation () with
        | None -> false
        | Some range -> not (range.FileName.EndsWith(".fsi"))

    override this.ExecutePsiTransaction(solution, progress) =
        use writeCookie = WriteLockCookie.Create(true)
        use disableFormatter = new DisableCodeFormatter()
        let letBindings = dataProvider.GetSelectedElement<ILetBindingsDeclaration>()
        let moduleDecl = ModuleDeclarationNavigator.GetByMember(letBindings)
        
        if isNotNull moduleDecl then
            let allDecls = moduleDecl.DeclaredElement.GetDeclarations() |> Seq.cast<INamedModuleDeclaration> |> Seq.toArray
            if allDecls.Length = 2 then
                let implDecl = Array.find (fun (nmd: INamedModuleDeclaration) ->
                    match nmd.Parent with
                    | :? IFSharpImplFile -> true
                    | _ -> false) allDecls
                let sigDecl = Array.find (fun (nmd: INamedModuleDeclaration) ->
                    match nmd.Parent with
                    | :? IFSharpSigFile -> true
                    | _ -> false) allDecls

                let elementFactory = sigDecl.CreateElementFactory()
                let binding = letBindings.Bindings.First()
                let refPat = binding.HeadPattern.As<IReferencePat>()
                let name = refPat.ReferenceName.Identifier.Name
                let symbolUse = refPat.GetFcsSymbolUse()
                if isNull symbolUse then () else

                let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
                let types = FcsTypeUtil.getFunctionTypeArgs true mfv.FullType
                let parameters = binding.ParametersDeclarations
                let rec getTypeName (t:FSharpType) : string =
                    if t.HasTypeDefinition then
                        if Seq.isEmpty t.GenericArguments then
                            t.TypeDefinition.DisplayName
                        else
                            let isPostFix =
                                set [| "list"; "option"; "array" |]
                                |> Set.contains t.TypeDefinition.DisplayName
                                
                            if isPostFix then
                                let ga = t.GenericArguments[0].GenericParameter
                                let tick = if ga.IsSolveAtCompileTime then "^" else "'"
                                $"{tick}{ga.DisplayName} {t.TypeDefinition.DisplayName}"
                            else
                                let args = Seq.map getTypeName t.GenericArguments |> String.concat ","
                                $"{t.TypeDefinition.DisplayName}<{args}>"
                    elif t.IsGenericParameter then
                        let tick = if t.GenericParameter.IsSolveAtCompileTime then "^" else "'"
                        $"{tick}{t.GenericParameter.DisplayName}"
                    elif t.IsFunctionType then
                        let rec visit (t: FSharpType) : string seq =
                            if  t.IsFunctionType then
                                Seq.collect visit t.GenericArguments
                            else
                                Seq.singleton (getTypeName t)
                        
                        visit t
                        |> String.concat " -> "
                        |> sprintf "(%s)"
                    elif t.IsTupleType then
                        t.GenericArguments
                        |> Seq.map getTypeName
                        |> String.concat " * "
                    else
                        "???"

                let namedParameters =
                    Seq.zip types parameters
                    |> Seq.map (fun (t, p) ->
                        let typeName = getTypeName t
                        match p.Pattern.IgnoreInnerParens() with
                        | DeclaredNameInPattern name -> $"{name}: {typeName}"
                        | :? ITuplePat as tuplePat ->
                            let typesInTuple = typeName.Split([|" * "|], StringSplitOptions.RemoveEmptyEntries)
                            if tuplePat.Patterns.Count = typesInTuple.Length then
                                Seq.zip tuplePat.PatternsEnumerable typesInTuple
                                |> Seq.map (fun (pat, t) ->
                                    match pat.IgnoreInnerParens() with
                                    | DeclaredNameInPattern name -> $"{name}: {t}"
                                    | _ -> t)
                                |> String.concat " * "
                            else
                                typeName
                        | _ -> typeName
                    )

                let signature =
                    match Seq.tryLast types with
                    | None -> getTypeName mfv.FullType
                    | Some returnType ->
                        seq { yield! namedParameters; yield getTypeName returnType}
                        |> String.concat " -> "

                let isInline =
                    match mfv.InlineAnnotation with
                    | FSharpInlineAnnotation.AlwaysInline -> true
                    | _ -> false
                
                let sigDeclNode : ITreeNode = elementFactory.CreateBindingSignature(isInline, name, signature)
                let lastChild = ModificationUtil.AddChildAfter(sigDecl.LastChild, NewLine(sigDeclNode.GetLineEnding()))
                let lastChild = ModificationUtil.AddChildAfter(lastChild, NewLine(sigDeclNode.GetLineEnding()))
                ModificationUtil.AddChildAfter(lastChild, sigDeclNode) |> ignore

            ()

        null
