// ${CHAR:Enter}
module Module =

   foo (fun _ ->
      bar (fun _ ->
         match 123 with
         | _ -> ()){caret}) |> ignore
