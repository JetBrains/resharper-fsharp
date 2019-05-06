module Module

open System

type String with
    member x.Bar(a) = x.Bar(a)
    member x.Bar(a, b) = x.Bar(a, b)

let s = ""
s.Bar{caret}(123)
