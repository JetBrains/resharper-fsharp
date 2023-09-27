namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.Deconstruction
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

type DeconstructPatternFix(error: UnionCaseExpectsTupledArgumentsError) =
    inherit FSharpQuickFixBase()

    let mutable deconstruction = None

    let tryCreate pattern =
        deconstruction <-
            match DeconstructionFromUnionCaseFields.TryCreate(pattern, false) with
            | null -> None
            | d -> Some(pattern, d)
        isNotNull deconstruction

    override this.IsAvailable _ =
        isValid error.Pat &&

        let parameters = error.Pat.Parameters
        parameters.Count = 1 &&

        let pattern = parameters[0]
        pattern :? IReferencePat && tryCreate pattern

    override this.Text =
        match deconstruction with
        | Some(_, deconstruction) -> deconstruction.Text
        | _ -> failwithf ""

    override x.ExecutePsiTransaction(_, _) =
        match deconstruction with
        | Some(pattern, deconstruction) ->
            FSharpDeconstruction.deconstruct true null deconstruction pattern
        | _ -> failwithf ""
