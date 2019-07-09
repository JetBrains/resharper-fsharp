type T() =
    let x = 1
    let f x = x
    let h = fun x -> x + 1
    member __.P1 = 1
    member __.P2 = fun x -> x
    member __.M1() = 1
    member __.M2(x: int) = x
    member __.M3(x, y) z (a, b, c) = x + y + string z + a + b + c
    interface System.IDisposable with
        member __.Dispose() = ()
