type T() =
    let i = 0

    member this.M(t{caret}: int * bool) =
        let _ = i
        ()
