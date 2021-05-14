// ${DIRECTION:Right}

match []{caret},
      Some () with | _ -> ()

                   | _ -> [1
                           2] |> ignore

                   | _ -> ignore
                              [1
                               2]
