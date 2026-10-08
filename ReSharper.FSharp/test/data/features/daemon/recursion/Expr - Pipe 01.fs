module Module

let rec f1 acc xs =
    match xs with
    | [] -> acc
    | _ :: tail -> tail |> f1 (acc + 1)

let rec f2 xs =
    match xs with
    | [] -> ()
    | _ :: tail -> tail |> f2

let rec f3 acc xs =
    match xs with
    | [] -> acc
    | _ :: tail -> f3 (acc + 1) <| tail

let rec f4 acc xs =
    match xs with
    | [] -> acc
    | _ :: tail -> (acc + 1, tail) ||> f4

let rec f5 acc xs =
    match xs with
    | [] -> acc
    | _ :: tail -> tail |> f5 (acc + 1) |> id

let rec f6 acc xs =
    match xs with
    | [] -> acc
    | _ :: tail -> tail |> List.map id |> f6 (acc + 1)
