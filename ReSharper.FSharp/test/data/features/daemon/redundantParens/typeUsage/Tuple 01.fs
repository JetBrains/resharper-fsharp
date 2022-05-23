module Module

type T1 = (int * int)
type T2 = (int * int) * int
type T3 = int * (int * int)
type T4 = (int -> int) * (int -> int)

type ST1 = (struct (int * int))
type ST2 = ((struct (int * int)))
type ST3 = (struct (int * int)) * unit
type ST4 = unit * (struct (int * int))
type ST5 = (struct (int * int) -> unit)
type ST6 = (struct (int * int) -> unit -> unit)
type ST7 = (struct (int * int)) -> unit
type ST8 = (struct (int * int)) -> unit -> unit
type ST9 = unit -> struct (int * int)
type ST10 = (struct (int * int)) * int -> unit
