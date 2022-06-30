type T =
    static member M(a, b) = ()

T.M(1,{caret} 2)
