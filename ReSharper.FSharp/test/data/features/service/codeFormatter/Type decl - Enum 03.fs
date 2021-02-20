namespace Ns1 // empty line after top level

//erge
type E =
    | A = 1

type R =
    { F: int
      F2: int }

namespace Ns2 // empty line after top level

type E1 =
    | A = 1

type E2 =
    | A = 1

exception E1
exception E2 of int

let x = 123
let y = 1

module Nested = // usually no empty line after nested module
    type E =
        | A = 1
