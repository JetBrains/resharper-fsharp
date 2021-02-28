module Module

type U =
    | A of (int -> int)
    | B of ((int -> int) * int) * int
    | C of ((int -> int) -> int) * int
    | D of (int * (int -> int)) * int
    | E of (int -> (int -> int)) * int

exception EA of (int -> int)
exception EB of ((int -> int) * int) * int
exception EC of ((int -> int) -> int) * int
exception ED of (int * (int -> int)) * int
exception EE of (int -> (int -> int)) * int
