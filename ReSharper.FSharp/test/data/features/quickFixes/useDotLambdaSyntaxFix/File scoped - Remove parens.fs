module Test

f(fun x{caret} -> x.ToString()) 
f x(fun x -> x.ToString())x

open System.Linq
[1;2;3] |> List.map (fun x -> x.ToString())
[1;2;3] |> List.map(fun x -> x.ToString())
[1;2;3] |> List.map (fun x -> x.ToString())|> List.map id
[1;2;3].Select(fun x -> x.GetHashCode()).Where(fun x -> x.Equals)
