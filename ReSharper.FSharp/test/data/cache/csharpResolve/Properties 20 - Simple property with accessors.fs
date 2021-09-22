namespace global

type T() =
    member x.A with get() = 1
                and set (x: int) = ()
