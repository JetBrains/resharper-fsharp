type A =
    | A of hello: string * hi: int * world: int

match A("", 1, 2) with
| A(hello = hello; hi = 2; world = 1; {caret}) -> ()
| _ -> ()
