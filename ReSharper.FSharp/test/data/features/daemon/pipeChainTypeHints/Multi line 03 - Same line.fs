[ 1; 2; 3; 4; 5; 6 ]
|> List.map (fun xs ->
    xs + 5
)
|> List.groupBy (fun x -> x / 2)
|> List.toSeq |> Seq.distinct
|> Set.ofSeq |>
Set.toSeq
|> Set.ofSeq
|> id
