module Module

let _ =
    query {
        join a in [] on ("" = a)
        join c in [] on ("" = "") 
        select a
    }
