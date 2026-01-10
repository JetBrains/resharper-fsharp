module Module

[<AbstractClass>]
type AbstractClass() =
    abstract member M1: unit -> unit
    default this.M1() = ()

    abstract member M2: int -> unit
    default this.M2(i: int) = ()

    abstract member M3<'P1>: int -> unit
    default this.M3<'P1>(i: int) = ()

    abstract member M4: 'P1 -> unit
    default this.M4(p: 'P1) = ()

    abstract member P1: int
    default this.P1 = 1

[<AbstractClass>]
type GenericAbstractClass<'TOuter>() =
    abstract member M1: unit -> unit
    default this.M1() = ()

    abstract member M2: int -> unit
    default this.M2(i: int) = ()

    abstract member M3<'P1>: int -> unit
    default this.M3<'P1>(i: int) = ()

    abstract member M4<'P1>: 'P1 -> unit
    default this.M4<'P1>(i: 'P1) = ()

    abstract member M5<'P1>: 'P1 * 'TOuter -> unit
    default this.M5<'P1>(i1: 'P1, i2: 'TOuter) = ()

    abstract member P1: 'TOuter
    default this.P1 = Unchecked.defaultof<'TOuter>

[<AbstractClass>]
type GenericAbstractClass2<'TSystem, 'TState, 'TInput, 'TOutput>() =
    abstract member M: state: 'TState * input: 'TInput -> bool
    default this.M(_, _) = true

type GenericClass<'TOuter>() =
    abstract member M: unit -> unit
    default this.M() = ()

    abstract member M2: int -> unit
    default this.M2(i: int) = ()
    
    abstract member M3<'P1>: int -> unit
    default this.M3<'P1>(i: int) = ()

    abstract member M4: 'TOuter -> unit
    default this.M4(p: 'TOuter) = ()
    
    abstract member M5<'P1>: 'P1 * 'TOuter -> unit
    default this.M5<'P1>(i1: 'P1, i2: 'TOuter) = ()
