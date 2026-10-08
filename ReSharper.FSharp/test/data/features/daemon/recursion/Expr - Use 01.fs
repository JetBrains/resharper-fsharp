module Module

let rec f1 x =
    use d = new System.IO.MemoryStream()
    f1 x

let rec f2 x =
    let d = new System.IO.MemoryStream()
    f2 x
