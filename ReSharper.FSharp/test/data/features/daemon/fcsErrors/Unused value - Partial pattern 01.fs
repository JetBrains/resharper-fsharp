module Module

match None, None with
| a, _
| _, a -> ()
| a -> ()

match None, None with
| a, _
| _, a -> a |> ignore
| a -> ()

match None, None with
| _, Some (a | a) -> ()
| _ -> ()
