module Module

match [] with
| _ ->
    []
    |> function [] -> ()

do
    function _ -> match [] with [] -> ()
    |> ignore
