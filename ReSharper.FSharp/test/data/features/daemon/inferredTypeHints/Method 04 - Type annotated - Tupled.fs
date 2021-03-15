type C () =
    member __.Subtract (x: int, y) =
        x - y

    member __.LambdaProp = fun (x: int, y) -> x - y
