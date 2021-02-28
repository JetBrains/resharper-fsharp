module Module

type U =
    | A of (int * int)
    | B of (int * int) * int

exception E1 of (int * int)
exception E2 of (int * int) * int
