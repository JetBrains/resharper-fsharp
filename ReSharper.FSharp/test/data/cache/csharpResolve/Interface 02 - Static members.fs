namespace global

[<Interface>]
type T1 =
    abstract P1: int
    static member P2 = 1

type T2 =
    interface
        abstract P1: int
        static member P2 = 1
    end

type T3<'T> =
    interface
        abstract P1: 'T
        static member P2 = Unchecked.defaultof<'T>
    end
