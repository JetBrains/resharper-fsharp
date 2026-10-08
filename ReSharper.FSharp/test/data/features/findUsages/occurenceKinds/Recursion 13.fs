module Module

let rec f acc xs =
    match xs with
    | [] -> acc
    | _ :: tail -> tail |> f (acc + 1)
