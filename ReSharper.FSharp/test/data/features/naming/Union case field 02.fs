module Module

type U =
    | A of theField: int

match A 123 with
| A (_ as x{caret}) -> ()
