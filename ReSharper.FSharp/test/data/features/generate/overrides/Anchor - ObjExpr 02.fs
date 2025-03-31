// ${KIND:Overrides}
// ${SELECT0:ToString():System.String?}

module Module

type T() =
    static member M() =
        { new obj() with{caret}
        }
