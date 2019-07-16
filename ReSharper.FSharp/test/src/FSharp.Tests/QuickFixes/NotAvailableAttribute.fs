namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.TestFramework

[<AutoOpen>]
module NotAvailable =
    // todo: make the const public in the framework
    let [<Literal>] NO_HIGHLIGHTINGS_FOUND_ERROR = "No error highlightings found for QuickFix"

type NotAvailableAttribute() =
    inherit ExpectedExceptionInsideSolutionAttribute(ExpectedMessage = NO_HIGHLIGHTINGS_FOUND_ERROR)
