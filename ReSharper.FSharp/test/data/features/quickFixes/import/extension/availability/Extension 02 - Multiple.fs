module A =
    type System.String with
        member _.FirstOrDefault() = ()

module B =
    type System.String with
        member _.FirstOrDefault() = ()

let _ = "".FirstOrDefault{caret}()
