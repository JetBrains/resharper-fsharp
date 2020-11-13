namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

type AddDiscriminatedUnionAllClauses(warning: MatchIncompleteWarning) =
    inherit FSharpQuickFixBase()

    let unionNameFromCase (pattern : IFSharpPattern) : string option =
        match pattern with
        | :? INamedPat as pat1 -> Some pat1.Identifier.Name
        | _ -> None
    let rec caseIsRestrictive (pattern: IFSharpPattern) (fsharpType: FSharpType) : bool =
        let restrictedDueToUnion =
            if fsharpType.TypeDefinition.IsFSharpUnion && pattern.As<INamedPat>() |> isNotNull then
                let unionLiteral =
                   fsharpType.TypeDefinition.UnionCases
                   |> Seq.tryFind (fun unionCase -> unionCase.DisplayName = (unionNameFromCase pattern).Value)
                match unionLiteral with
                | Some literal ->
                    match literal.UnionCaseFields |> Seq.toList with
                    | [] -> false
                    | caseFields ->
                        // If there are case fields, valid F# should have something in this pattern, unless
                        // there's a catchall parameter - deal with that...
                        let parameters = pattern.As<IParametersOwnerPat>().Parameters |> Seq.toList
                        List.zip parameters caseFields
                            |> List.forall(fun (pattern, field) -> caseIsRestrictive pattern field.FieldType |> not)
                | None -> false
            else
                // Not restrictive with the _ pattern, otherwise restrictive
                pattern.As<IWildPat>() |> isNull
        // If you're not a union, you could be restricted if you have any sort of pattern that constrains the
        // match: don't try to actually evaluate completeness, just a heuristic
        let restrictedForAnotherReason =
            not fsharpType.TypeDefinition.IsFSharpUnion && pattern.As<INamedPat>() |> isNull
            
        restrictedDueToUnion
        || restrictedForAnotherReason
        
    
    let isCaseNonExhaustive (unionCase: FSharpUnionCase) (relevantMatchClauses: IMatchClause list) : bool =
        // TODO MC
        relevantMatchClauses |> List.forall(fun clause -> caseIsRestrictive clause.Pattern unionCase.ReturnType)
    
    let nonExhaustiveUnionCases (unionCases : FSharpUnionCase seq) (allMatchClauses : IMatchClause seq) : FSharpUnionCase list =
        let allMatchClauses = allMatchClauses |> Seq.toList
        // TODO MC: Cover wild cases
        // TODO MC: Unfold?
        unionCases
        |> Seq.map (fun case ->
            (case,
             allMatchClauses
             |> List.filter (fun clause -> case.DisplayName = (unionNameFromCase clause.Pattern).Value)))
        |> Seq.filter (fun (case, clauses) -> isCaseNonExhaustive case clauses)
        |> Seq.map fst
        |> Seq.toList
    
    override x.Text = "Autogenerate all discriminated union cases"

    override x.IsAvailable _ =
        let fsharpType = warning.Expr.Expression.TryGetFcsType()
        
        isValid warning.Expr
        && (isNotNull fsharpType)
        && fsharpType.HasTypeDefinition
        && fsharpType.TypeDefinition.IsFSharpUnion
        && (nonExhaustiveUnionCases fsharpType.TypeDefinition.UnionCases warning.Expr.Clauses |> List.isEmpty |> not)

    override x.ExecutePsiTransaction _ =
        let expr = warning.Expr
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        let factory = expr.CreateElementFactory()
        use enableFormatter = FSharpRegistryUtil.AllowFormatterCookie.Create()

        let typeDefinition = warning.Expr.Expression.TryGetFcsType().TypeDefinition
        
        // TODO: Make an extension on FSharpUnionCase
        let prependedName =
            if typeDefinition.Attributes
               |> Seq.where (fun x -> x.GetType() = typeof<RequireQualifiedAccessAttribute>)
               |> Seq.isEmpty
            then
                ""
            else
                typeDefinition.DisplayName + "."
        let nodesToAdd =
            nonExhaustiveUnionCases typeDefinition.UnionCases warning.Expr.Clauses
            |> Seq.collect(fun case ->
                [NewLine(expr.GetLineEnding()) :> ITreeNode
                 factory.CreateMatchClause(prependedName + case.DisplayName, case.HasFields) :> ITreeNode])
            |> Seq.toList
        
        addNodesAfter expr.WithKeyword nodesToAdd |> ignore
