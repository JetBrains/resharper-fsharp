let (|>) (x : 'a) (f : 'a -> 'b) = f x
[1;2;3] |> List.toSeq