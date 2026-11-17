module Module

let foo = "foo"
let t = 10, foo

let s1 = foo |> sprintf "%s"
let s2 = sprintf "%s" <| foo
let s3 = foo |> sprintf "%d %s" 10
let s4 = (10, foo) ||> sprintf "%d %s"
let s5 = sprintf "%d %s" <|| (10, foo)
let s6 = foo |> sprintf "%s" |> ignore

let n1 = t ||> sprintf "%d %s"
let n2 = foo |> sprintf "%d %s"

let n3 =
    [1; 2]
    |> List.sum
    |> sprintf "%d"

let s7 =
    sprintf "%d" (
        [1; 2]
        |> List.sum)
