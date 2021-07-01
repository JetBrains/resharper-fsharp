type GU<'T> =
    | C of 'T[]

match C([| 1 |]) with
| _{caret} -> ()
