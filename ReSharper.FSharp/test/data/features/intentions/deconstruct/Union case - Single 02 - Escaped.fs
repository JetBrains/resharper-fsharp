type U =
    | ``A B`` of field: int

match ``A B`` 1 with
| _{caret} -> ()
