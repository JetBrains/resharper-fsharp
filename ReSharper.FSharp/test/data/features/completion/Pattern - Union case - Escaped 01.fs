// ${COMPLETE_ITEM:B C}
module Module

type U =
    | A
    | ``B C`` of int

match A with
| {caret}
