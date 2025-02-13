module Test

open System

let f x =
    let g x = ()
    let x = fun y -> ()
    for x in [""] do ()
    match 5 with x -> () 
    { new IFormattable with
        member x.ToString(format, provider) = "" }

fun x -> ()

match 5 with
| x when let y = 5 in true ->
    let z = 3
    ()

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
