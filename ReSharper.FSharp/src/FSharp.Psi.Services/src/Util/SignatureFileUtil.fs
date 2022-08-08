module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.SignatureFile

open System
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl

let private (|DeclaredNameInPattern|_|) (pat:IFSharpPattern) =
    match pat with
    | :? ILocalReferencePat as pat -> Some pat.DeclaredName
    | :? ITypedPat as tp ->
        match tp.Pattern.IgnoreInnerParens() with
        | :? ILocalReferencePat as pat -> Some pat.DeclaredName
        | _ -> None
    | _ -> None

let tryMkBindingSignature
    (letBindings: ILetBindings)
    (moduleDecl: IModuleDeclaration)
    : (IBindingSignature * IFSharpSigFile) option
    =
    if isNull moduleDecl then None else
    
    let allDecls = moduleDecl.DeclaredElement.GetDeclarations() |> Seq.cast<INamedModuleDeclaration> |> Seq.toArray
    if allDecls.Length <> 2 then None else

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
    if isNull symbolUse then None else

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
    
    let accessibility =
        let vis = symbolUse.Symbol.Accessibility
        if vis.IsPrivate then
            Some "private"
        elif vis.IsInternal then
            Some "internal"
        else
            None
    
    let sigDeclNode : IBindingSignature = elementFactory.CreateBindingSignature(isInline, accessibility, name, signature)
    let signatureFile = sigDecl.Parent :?> IFSharpSigFile

    Some (sigDeclNode, signatureFile)