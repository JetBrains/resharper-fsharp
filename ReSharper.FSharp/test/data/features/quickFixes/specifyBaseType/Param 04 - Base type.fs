//${OCCURRENCE:I1}

type I1 =
    interface end

type I2 =
    inherit I1

type T1() =
    class end

type T2() =
    inherit T1()

    interface I2

let f x =
    match x with
    | :? T2{caret} -> ()
