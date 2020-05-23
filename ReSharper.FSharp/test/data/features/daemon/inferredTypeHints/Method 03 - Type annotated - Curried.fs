type C () =
    member __.Subtract x (y : int) =
        x - y

    member __.LambdaProp = fun x (y: int) -> x - y
