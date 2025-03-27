module Module

open FSharp.Data

let [<Literal>] s = "hello"
type A = JsonProvider<s{caret}>
