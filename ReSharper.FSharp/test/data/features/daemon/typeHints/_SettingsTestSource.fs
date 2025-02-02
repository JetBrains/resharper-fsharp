module Test

open System

let f x =
    let g x = ()
    let x = fun y -> ()
    for x in [""] do ()
    { new IFormattable with
        member x.ToString(format, provider) = "" }

fun x -> ()

type A(x) =
    do
        let x = 3
        ()

    let array = [|""|]

    new(x, y) = A(x + y)

    member _.P1 =
        let x = 1
        x

    member _.P2 = id

    member val P3 = 3 with get, set

    member x.P4
        with get index =
            let x = array[index]
            x
        and set index value =
            array[index] <- value

    member _.M(x) (y, z) =
        let res = x + y + z
        res
