//${OCCURRENCE:System.IDisposable}

type I2 =
    inherit System.IDisposable

let f x y =
    match x, y with
    | :? I2{caret}, y as z -> ()
