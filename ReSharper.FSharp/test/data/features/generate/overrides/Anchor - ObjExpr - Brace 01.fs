// ${KIND:Overrides}
// ${SELECT0:ToString():System.String?}

module Module

type T() =
    static member M() =
        {caret}{ new obj() with }
