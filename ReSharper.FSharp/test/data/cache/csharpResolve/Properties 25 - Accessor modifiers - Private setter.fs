namespace global

type T() =
    member x.A with get() = 1
               and private set (_: int) = ()
