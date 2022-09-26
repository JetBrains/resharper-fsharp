namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

//  Library.fs(3, 8): [FS3218] The argument names in the signature 'c' and implementation 'b' do not match. The argument name from the signature file will be used. This may cause problems when debugging or profiling.
// Implementation is source of truth in this quick fix.
// There should be another quickfix to change the implementation file.

type UpdateParameterNameInSignatureFix(warning: ArgumentNameMismatchWarning) =
    inherit FSharpQuickFixBase()

    let declaredElementAndParameterPatterns =
        let rec getTopLevelPattern (p: IFSharpPattern) : IFSharpPattern =
            let p = p.IgnoreParentParens()

            let typedPat = TypedPatNavigator.GetByPattern(p)
            if isNotNull typedPat then getTopLevelPattern typedPat else
            
            let attribPat = AttribPatNavigator.GetByPattern(p)
            if isNotNull attribPat then getTopLevelPattern attribPat else
            
            let optionalPat = OptionalValPatNavigator.GetByPattern(p)
            if isNotNull optionalPat then getTopLevelPattern optionalPat else p

        let p = getTopLevelPattern warning.Pattern

        let tuplePat = TuplePatNavigator.GetByPattern(p)
        let topLevelPat : IFSharpPattern = if isNotNull tuplePat then tuplePat else p
        let topLevelPat = topLevelPat.IgnoreParentParens()

        let decl = ParameterOwnerMemberDeclarationNavigator.GetByParameterPattern(topLevelPat)

        if isNull decl then
            None
        else
            let indexInTuple = if isNotNull tuplePat then Some (tuplePat.Patterns.IndexOf(p)) else None
            let indexOfParameterInBinding = decl.ParameterPatterns.IndexOf(topLevelPat)
            Some (decl.DeclaredElement, indexOfParameterInBinding, indexInTuple)

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(warning.Pattern.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        
        match declaredElementAndParameterPatterns with
        | None -> ()
        | Some (declaredElement, indexOfParameterInBinding, indexInTuple) ->

        // TODO: rename Declarations
        let declarations =
            // it has two declarations: one signature, one implementation
            if  isNull declaredElement then Seq.empty else declaredElement.GetDeclarations()

        let returnTypeOption = 
            declarations
            |> Seq.tryPick (fun d ->
                    match d with 
                    | :? IReferencePat as rp ->
                        BindingSignatureNavigator.GetByHeadPattern(rp)
                        |> Option.ofObj
                        |> Option.map (fun bs -> bs.ReturnTypeInfo.ReturnType)
                    | :? IMemberSignature as ms ->
                        Some(ms.ReturnTypeInfo.ReturnType)
                    | :? IConstructorSignature as cs ->
                        Some(cs.ReturnTypeInfo.ReturnType)
                    | _ -> None)

        match returnTypeOption with
        | None -> ()
        | Some returnType ->
            let rec loop times (t: ITypeUsage) =
                if times = 0 then
                    match t with
                    | :? IFunctionTypeUsage as ftu -> ftu.ArgumentTypeUsage
                    | _ -> t
                else
                match t with
                | :? IFunctionTypeUsage as ftu ->
                    loop (times  - 1) ftu.ReturnTypeUsage
                | _ -> t
            
            let signatureTypeAtIndex = loop indexOfParameterInBinding returnType
            let signatureType =
                match signatureTypeAtIndex, indexInTuple with
                | :? ITupleTypeUsage as ttu, Some indexInTuple ->
                    ttu.Items.[indexInTuple]
                | _ -> signatureTypeAtIndex

            match signatureType with
            | :? IParameterSignatureTypeUsage as pstu ->
                if isNotNull pstu.Identifier then
                    pstu.Identifier.ReplaceIdentifier(warning.ImplementationParameterName)
            | _ -> ()

    override this.IsAvailable _ =
        isValid warning.Pattern && Option.isSome declaredElementAndParameterPatterns
    override this.Text = "Update parameter name in signature file."
