type 'a MyRecord = { MyOption : 'a option }

let run () =
    let x = { MyOption = Some 15. }
    match x with
    | { MyOption = None } -> ()
    | { MyOption = Some x } -> ()
