type GU<'T1, 'T2> =
    | C of 'T1 * 'T2 option

match C(1, Some "") with
| _{caret} -> ()
