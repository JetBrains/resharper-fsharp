type R = { A: int; B: int }
module foo =
    let r (input : int option) : (R option) =
        input
        |> Option.map(fun x -> {})