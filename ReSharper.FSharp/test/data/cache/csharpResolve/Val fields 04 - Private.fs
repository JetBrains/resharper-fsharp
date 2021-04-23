namespace global

type T() =
    [<DefaultValue>]
    val mutable ImplicitPublic: int

    [<DefaultValue>]
    val mutable public Public: int

    [<DefaultValue>]
    val mutable private Private: int
