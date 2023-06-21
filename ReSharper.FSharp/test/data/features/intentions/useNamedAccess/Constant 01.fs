type Foo =
    | Foo of a:int * b:string * c:float

let a (b: Foo) =
    match b with
    | Foo(2, "blah", 2.1{caret}) -> ()
    | _ -> ()
