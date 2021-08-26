namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.Deconstruction
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

type DeconstructPatternFix(error: UnionCaseExpectsTupledArgumentsError) =
    inherit FSharpQuickFixBase()

    let mutable deconstruction = null

    let tryCreate pattern =
        deconstruction <- DeconstructionFromUnionCaseFields.TryCreate(pattern, false)
        isNotNull deconstruction

    override this.IsAvailable _ =
        isValid error.Pat &&

        let parameters = error.Pat.Parameters
        parameters.Count = 1 &&

        let pattern = parameters.[0]
        pattern :? IReferencePat && tryCreate pattern

    override this.Text = deconstruction.Text

    override x.ExecutePsiTransaction(_, _) =
        FSharpDeconstruction.deconstruct deconstruction
