[ 1; 2; 3; 4; 5; 6 ]
|> List.groupBy (fun i -> i / )
|> List.toSeq
