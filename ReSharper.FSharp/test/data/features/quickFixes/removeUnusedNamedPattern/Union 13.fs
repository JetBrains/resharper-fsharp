type U =
    | U of a:int

match U(a = 1) with
| A(a = {caret}a) -> ()
