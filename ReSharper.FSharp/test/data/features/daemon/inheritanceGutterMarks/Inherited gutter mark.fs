module Module

type IInterface =
    abstract Foo: int
    abstract M: 'T -> unit
    abstract M1: int -> unit
    abstract M1: double -> unit

type internal T() =
    interface IInterface with
        member x.Foo = 123
        member x.M(_) = ()
        member x.M1(_: int) = ()
        member x.M1(_: double) = ()

type U  =
    | A of int
    | B of int

type Base() =
    class
    end

type Inheritred() =
    inherit Base()
