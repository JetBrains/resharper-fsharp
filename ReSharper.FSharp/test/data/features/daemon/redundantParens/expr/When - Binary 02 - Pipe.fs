module M

match () with
| _ when (true |> id) -> ()
| _ when (true |> id) |> id -> ()
| _ when (true |> id |> id) -> ()
| _ when ((true |> id) |> id) -> ()
| _ when (true |> (id |> id)) -> ()

| _ when (id <| true) -> ()
| _ when id <| (id <| true) -> ()
| _ when (id <| id <| true) -> ()
| _ when (id <| (id <| true)) -> ()
| _ when ((id <| id) <| true) -> ()
