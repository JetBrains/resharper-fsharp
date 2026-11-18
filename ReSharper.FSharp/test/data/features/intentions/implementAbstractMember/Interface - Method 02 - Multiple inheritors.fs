module Module

type I =
    abstract M{caret}: int -> unit

type T1() =
    interface I

type T2() =
    interface I
