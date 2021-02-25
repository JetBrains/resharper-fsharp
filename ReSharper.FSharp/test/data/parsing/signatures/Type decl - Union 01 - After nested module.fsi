module Module

module CancellableAutoOpens =
    val cancellable: CancellableBuilder

type Eventually<'T> = 
    | NotYetDone of (Eventually<int>)
