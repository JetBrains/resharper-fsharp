module A

    module B =
            
        [<Literal>]
        val SymbolFromB : int = 42

    // B is not open here
    
    [<Literal>]
    val a : int = 23
