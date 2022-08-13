// ${CHAR:<}
module Module

type A =
    /// {caret}
    member x.M(y: int, (a: int, b: int), _: int, [<NotNull>] i: int) (?z) = ()
