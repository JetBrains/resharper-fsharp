namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.Symbols
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.RdBackend.Common.Env
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open type JetBrains.Diagnostics.Assertion
open JetBrains.Rider.Backend.Env

[<ContextAction(Name = "UseNamedAccess", Group = "F#", Description = "Use named access inside a DU pattern")>]
[<ZoneMarker(typeof<IDocumentModelZone>, typeof<ILanguageFSharpZone>, typeof<IProjectModelZone>, typeof<IResharperHostCoreFeatureZone>, typeof<IRiderFeatureEnvironmentZone>, typeof<PsiFeaturesImplZone>)>]
type UseNamedAccessAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)
    
    let mutable names = Array.empty
    let mutable patterns : IFSharpPattern seq = Seq.empty

    override x.Text = 
        let pattern = dataProvider.GetSelectedElement<IParametersOwnerPat>()
        $"Use named fields for '{pattern.ReferenceName.ShortName}'"

    override x.IsAvailable _ =
        let pattern = dataProvider.GetSelectedElement<IParametersOwnerPat>()
        if isNull pattern then false else
        // We expect a single IParenPat here
        if pattern.Parameters.Count <> 1 then false else
        let parenPat = pattern.ParametersEnumerable.SingleItem.As<IParenPat>()
        if isNull parenPat then false else

        // Ignore multiline patterns for now
        if parenPat.StartLine <> parenPat.EndLine then false else

        if isNull pattern.ReferenceName then false else
        // We need to be sure that the pattern is a DU with all named fields
        let fcsUnionCase = pattern.ReferenceName.Reference.GetFcsSymbol().As<FSharpUnionCase>()
        if isNull fcsUnionCase then false else
            
        names <-
            fcsUnionCase.Fields
            |> Seq.choose (fun field -> if field.IsNameGenerated then None else Some field.Name)
            |> Seq.toArray
        
        // All fields need to be named
        if names.Length <> fcsUnionCase.Fields.Count then false else

        // The amount of fields needs to match with the current pattern length
        match parenPat.Pattern with
        | :? ITuplePat as tuplePat when names.Length = tuplePat.Patterns.Count ->
            patterns <- tuplePat.PatternsEnumerable
            true
        | singlePat when names.Length = 1 ->
            patterns <- Seq.singleton singlePat
            true
        | _ -> false

    override x.ExecutePsiTransaction _ : unit =
        let pattern = dataProvider.GetSelectedElement<IParametersOwnerPat>().NotNull()
        use writeCookie = WriteLockCookie.Create(pattern.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        
        let rec visit (pat: IFSharpPattern) =
            match pat.IgnoreInnerParens() with
            | :? IWildPat-> None
            | :? ITuplePat as tuplePat ->
                let hasAnyPatterns = tuplePat.Patterns |> Seq.exists (visit >> Option.isSome)
                if hasAnyPatterns then Some pat else None
            | _ -> Some pat
        
        let usedFieldsWithPatterns: (string * IFSharpPattern)[] =
            (names, patterns)
            ||> Seq.zip
            |> Seq.choose (fun (name, pat) -> visit pat |> Option.map (fun pat -> name, pat))
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

module MissingAssemblyReferenceWorkaround =
    type T(a: IResharperHostCoreFeatureZone, b: IRiderFeatureEnvironmentZone) =
        class end
