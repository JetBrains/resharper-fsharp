module Module

let error = SomeType.Error()

let _ =
    match None with
    | Some x -> Ok x
    | None -> Error "Error"
