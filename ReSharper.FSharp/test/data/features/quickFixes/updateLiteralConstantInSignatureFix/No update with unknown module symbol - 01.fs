module A

    module B =
            
        [<Literal>]
        let SymbolFromB = 42

    open B

    [<Literal>]
    let a{caret} = SymbolFromB
