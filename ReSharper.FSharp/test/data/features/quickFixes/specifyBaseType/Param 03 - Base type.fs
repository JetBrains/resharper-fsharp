//${OCCURRENCE:T1}

type T1() =
    class end

type T2() =
    inherit T1()

let f x =
    match x with
    | :? T2{caret} -> ()
