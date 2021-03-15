let outer z =
    let x = [y; y+2]
    let y = [|z; z+1|]
    let u = z + 1
    let ts = 1, "hi", u
    ()
