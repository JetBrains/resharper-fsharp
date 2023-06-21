namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi
open type JetBrains.Diagnostics.Assertion

[<ContextAction(Name = "UseNamedAccess", Group = "F#", Description = "Use named access inside a DU pattern")>]
type UseNamedAccessAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)
    
    let mutable names = Array.empty
    let mutable tuplePat : ITuplePat = null

    override x.Text = "Use named access"

    override x.IsAvailable _ =
        let pattern = dataProvider.GetSelectedElement<IParametersOwnerPat>()
        if isNull pattern then false else
        // We expect a single IParenPat here
        if pattern.Parameters.Count <> 1 then false else
        let parenPat =
            match pattern.Parameters.[0] with
            | :? IParenPat as parenPat -> parenPat
            | _ -> null

        if isNull parenPat then false else

        // Ignore multiline patterns for now
        if parenPat.StartLine <> parenPat.EndLine then false else

        // This should only work when there are multiple items
        tuplePat <- parenPat.Pattern :?> ITuplePat
        if isNull tuplePat then false else
        
        let hasReplaceablePatterns =
            tuplePat.Patterns
            |> Seq.exists (fun p ->
                match p with
                | :? IWildPat
                | :? ILiteralPat -> true
                | _ -> false)
    
        // We need something that is currently ignored
        if not hasReplaceablePatterns then false else

        // We need to be sure that the pattern is a DU with all named fields
        let fcsUnionCase = pattern.ReferenceName.Reference.GetFcsSymbol().As<FSharpUnionCase>()
        if isNull fcsUnionCase then false else
            
        names <-
            fcsUnionCase.Fields
            |> Seq.choose (fun field -> if field.IsNameGenerated then None else Some field.Name)
            |> Seq.toArray
        
        // All fields need to be named
        if names.Length <> fcsUnionCase.Fields.Count then false else
        // The amount of fields needs to match with the tuple length
        if names.Length <> tuplePat.Patterns.Count then false else

        true

    override x.ExecutePsiTransaction _ : unit =
        let pattern = dataProvider.GetSelectedElement<IParametersOwnerPat>().NotNull()
        use writeCookie = WriteLockCookie.Create(pattern.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        
        let usedFieldsWithPatterns: (string * IFSharpPattern)[] =
            (names, tuplePat.PatternsEnumerable)
            ||> Seq.zip
            |> Seq.choose (fun (name, pat) ->
                match pat with
                | :? IWildPat-> None
                | _ -> Some (name, pat))
            |> Seq.toArray

        let factory = pattern.CreateElementFactory()
        let sourceText =
            let fields =
                usedFieldsWithPatterns
                |> Array.map (fun (name,_) -> $"{name} = _")
                |> String.concat "; "
            $"{pattern.Identifier.Name}({fields})"
        let namedPattern = factory.CreatePattern(sourceText, false)
        
        match namedPattern with
        | :? IParametersOwnerPat as ownerPat ->
            assert (ownerPat.Parameters.Count = 1)

            match ownerPat.Parameters.[0] with
            | :? INamedUnionCaseFieldsPat as namedUnionCaseFieldsPat ->
                for name, pat in usedFieldsWithPatterns do
                    let fieldPat =
                        namedUnionCaseFieldsPat.FieldPatterns
                        |> Seq.tryFind (fun fieldPat -> fieldPat.ReferenceName.Identifier.Name = name)
                    
                    match fieldPat with
                    | None -> ()
                    | Some fieldPat -> ModificationUtil.ReplaceChild(fieldPat.Pattern, pat) |> ignore

                ModificationUtil.ReplaceChild(pattern.Parameters.[0], namedUnionCaseFieldsPat)
                |> ignore
            | _ -> ()
        | _ -> ()
