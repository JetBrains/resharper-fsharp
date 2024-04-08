()
|> id

()
|> (id)

1
|> ignore

[1, 2]
|> dict

[1]
|> List.iter ignore

[1]
|> List.map ignore

5
|> Some



let f _ _ = ()

1
|> f

1
|> f 1



type A =
    static member F = fun _ -> 5
    static member F1 = fun _ -> fun _ -> ()

1
|> A.F

1
|> A.F1

1
|> A.F1 1



open System.Collections.Generic

[1]
|> List
