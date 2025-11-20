namespace global

type T() =
    member x.A with private get() = 1
               and set (_: int) = ()
