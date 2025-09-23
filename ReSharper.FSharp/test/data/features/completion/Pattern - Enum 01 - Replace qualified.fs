// ${ABSENT_ITEM:A}
module Module

type E =
    | A = 1

match E.A with
| E.{caret}A -> ()
