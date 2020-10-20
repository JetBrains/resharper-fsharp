namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.QuickDock

open JetBrains.ReSharper.Host.Features.Daemon.Tooltips.Request
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings

type FSharpQuickDocRequest() =
    inherit HighlightingBasedQuickDocTooltipRequest<FSharpIdentifierHighlighting>()
