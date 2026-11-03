// ${FILTER_ITEMS:B}
module Module

module Nested =
    type U =
        | A
        | B of int

open Nested

let x = {caret}
