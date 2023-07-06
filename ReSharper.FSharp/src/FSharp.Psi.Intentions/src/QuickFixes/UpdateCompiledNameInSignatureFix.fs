namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi

type UpdateCompiledNameInSignatureFix(error: ValueNotContainedMutabilityCompiledNamesDifferError) =
    inherit FSharpQuickFixBase()

    override this.IsAvailable(cache) = failwith "todo"
    override this.Text = failwith "todo"
