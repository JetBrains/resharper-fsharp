module Module

match [] with
| [] -> ()

match [] with
| [] ->
    match [] with
    | [] -> ()
| _ -> ()
