let f x =
    match x with
    | :? System.IDisposable{caret} -> ()
