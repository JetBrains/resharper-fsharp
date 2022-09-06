// ${CHAR:<}
module Module

type A =
    /// {caret}
    abstract member Using: resource:'T * binder:('T -> Async<'U>) -> Async<'U> when 'T :> System.IDisposable
