module Module

type U =
    | A of theField: int * string

match A 123 with
| A (_{caret}, _) -> ()
