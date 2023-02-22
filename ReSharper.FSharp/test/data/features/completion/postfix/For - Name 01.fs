// ${COMPLETE_ITEM:for}
module Module

let f x =
    x
    |> List.map (fun mice -> mice.{caret})
    |> ignore
