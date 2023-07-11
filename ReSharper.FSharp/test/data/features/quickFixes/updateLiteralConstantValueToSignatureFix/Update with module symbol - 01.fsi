module A

    module B =

        [<Literal>]
        val SymbolFromB : int = 42

    open B

    [<Literal>]
    val a : int = 23
