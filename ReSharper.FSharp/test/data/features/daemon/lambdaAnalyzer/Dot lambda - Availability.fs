// Available
ignore <| fun x -> x.ToString()
ignore <| fun x -> x   .    ToString()
ignore <| fun x -> (x).ToString()
ignore <| fun (x) -> x.ToString()
ignore <| fun x -> (x.ToString())
ignore <| fun x -> (x.ToString())
ignore <| fun x -> x.Prop[0]
ignore <| fun x -> x.Prop.[0]
[1] |> (fun x -> x.ToString(fun x -> x.ToString())) |> ignore
[1] |> (fun x -> x.Equals(let x = 5 in x)) |> ignore
type A1 = member _.M() = fun x -> x.ToString()
let f (a, (b, c)) = fun x -> fun x -> x.ToString()
match 1 with _ -> fun x -> x.ToString()
let _ = let _ = 3 in fun x -> x.ToString()

type A = static member M(x: int -> string) = ()
A.M(fun x -> x.ToString())

// Not available
ignore <| fun x -> x.Equals(x)
ignore <| fun x -> x.ToString ()
ignore <| fun x -> y.ToString()
ignore <| fun x -> (x.ToString)()
[1] |> (fun x -> x[0]) |> ignore
[1] |> (fun x -> x.[0]) |> ignore
ignore <| fun x -> x.ToString()[0]
ignore <| fun (x, y) -> x.ToString()
[1] |> (fun x -> x.Select(_.ToString())) |> ignore 
[1] |> _.Select(fun x -> x.ToString()) |> ignore
(fun _ -> fun x -> x.ToString()) |> ignore
(fun _ -> fun y -> fun x -> x.ToString()) |> ignore
(fun (a, (b, _)) -> fun y -> fun x -> x.ToString()) |> ignore
type A2 = member _.M _ = fun x -> x.ToString()
let g (a, (b, Some _)) = fun x -> fun x -> x.ToString()

open System
open System.Linq.Expressions
type A with static member M(x: int, y: Func<int, string>) = ()
type A with static member M(x: string, y: Expression<Func<int, string>>) = ()
A.M(1, fun x -> x.ToString())
A.M("", fun x -> x.ToString())
