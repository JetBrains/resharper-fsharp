type Foo =
    | Meh of int * string
    | Bar of a:int * b:string * c:float

let a (b: Foo) =
    match b with
    | Meh (_, "") -> ()
    | Bar(
            2, 
            "blah", 
            {off}_
        ) -> ()
    | _ -> ()
