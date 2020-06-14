open System

let func f g x =
    let mutable a = String.Equals(x, "lol")
    let mutable mf = f
    let b = mf a
	a <- b
    mf <- not
    g (mf a) b x