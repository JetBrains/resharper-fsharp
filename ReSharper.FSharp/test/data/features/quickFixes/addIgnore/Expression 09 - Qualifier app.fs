type T =
    static member M() = ""

do
    T.M().Length{caret}
