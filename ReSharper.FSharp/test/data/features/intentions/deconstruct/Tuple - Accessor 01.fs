type T() =
    let i = 0

    member this.P with set (a{caret}: int * string) = 
        ignore i
