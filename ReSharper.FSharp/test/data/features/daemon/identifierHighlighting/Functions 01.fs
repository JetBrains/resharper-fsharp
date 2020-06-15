open System

let func f g (x : string) =
    let eq = Object.ReferenceEquals(x, null)
    let mutable mf = f
    g (mf x) eq