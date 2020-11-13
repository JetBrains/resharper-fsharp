// ${KIND:Overrides}
// ${SELECT0:ToString():System.String}

type T() =
    class
        member x.P1 = 1
    end
    {caret}member x.P2 = 1
