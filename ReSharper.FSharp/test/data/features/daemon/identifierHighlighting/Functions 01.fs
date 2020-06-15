let func f g (x : string) =
    let mutable mf = f
    g (mf x) (Object.Equals(x, null))