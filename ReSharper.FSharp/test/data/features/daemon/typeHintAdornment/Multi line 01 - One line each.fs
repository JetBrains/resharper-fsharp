[ 1; 2; 3; 4; 5; 6 ]
|> List.groupBy (fun i -> i / 2)
|> List.map (fun (group, xs) -> xs.Length)
|> List.toSeq
