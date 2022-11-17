//${OCCURRENCE:I1}

type I1 =
    interface end

type I2 =
    inherit I1

let f x =
    match x with
    | :? I2{caret} -> ()
