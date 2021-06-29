namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.TestFramework

module ErrorText =
    // todo: make the const public in the framework
    let [<Literal>] NoHighlightingsFoundError = "No error highlightings found for QuickFix"
    let [<Literal>] NotAvailable = "Not available"
    let [<Literal>] ActionNotAvailable = "No items for Available action!"

type NoHighlightingFoundAttribute() =
    inherit ExpectedExceptionInsideSolutionAttribute(ExpectedMessage = ErrorText.NoHighlightingsFoundError)

type NotAvailableAttribute() =
    inherit ExpectedExceptionInsideSolutionAttribute(ExpectedMessage = ErrorText.NotAvailable)

type ActionNotAvailableAttribute() =
    inherit ExpectedExceptionInsideSolutionAttribute(ExpectedMessage = ErrorText.ActionNotAvailable)
