module Module

let rec f1 () = async {
    return! f1 ()
}

let rec f2 () = async {
    do! f2 ()
}

let rec f3 () = seq {
    yield 1
    yield! f3 ()
}

let rec f4 () = async {
    let! x = async { return 1 }
    return! f4 ()
}

let rec f5 () = async {
    use! d = async { return new System.IO.MemoryStream() }
    return! f5 ()
}

let rec f6 () = async {
    return! f6 ()
    return ()
}

let rec f7 () =
    [ 1 ] |> List.map (fun _ -> async { return! f7 () })
