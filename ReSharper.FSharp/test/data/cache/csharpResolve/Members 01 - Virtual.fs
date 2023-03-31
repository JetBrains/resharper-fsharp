namespace global

type I =
    abstract M1: int -> unit
    abstract P1: int


type T1() =
    abstract M1: int -> unit
    default this.M1(i: int) = ()

    abstract P1: int
    default this.P1 = 1

    member this.M2(i: int) = ()
    member this.P2 = 1



type T2() =
    abstract M1: int -> unit
    default this.M1(i: int) = ()

    abstract P1: int
    default this.P1 = 1

    member this.M2(i: int) = ()
    member this.P2 = 1

    interface I with
        member this.M1(i: int) = this.M1(i)
        member this.P1 = this.P1


[<AbstractClass>]
type T3() =
    abstract M1: int -> unit
    abstract P1: int

    member this.M2(i: int) = ()
    member this.P2 = 1

    interface I with
        member this.M1(i: int) = this.M1(i)
        member this.P1 = this.P1
