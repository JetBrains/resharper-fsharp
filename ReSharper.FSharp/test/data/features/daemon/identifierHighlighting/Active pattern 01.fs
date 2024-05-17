module Module

let (|A|_|) x = Some ()
let (|B|C|) x = if x then B else C
let (|D|) x = x
let (|  ``E``  | F |) x = if x then E else F

module Nested =
    let (|G|_|) x = x

let _ = (|A|_|)
let _ = Nested.(|G|_|)

match () with
| D (|A|_|) -> ()
| D Nested.(|G|_|) -> ()
