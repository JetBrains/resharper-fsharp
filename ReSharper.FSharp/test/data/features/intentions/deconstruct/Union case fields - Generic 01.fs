type GU<'T> =
    | C of 'T * int

match C(1, 2) with
| _{caret} -> ()
