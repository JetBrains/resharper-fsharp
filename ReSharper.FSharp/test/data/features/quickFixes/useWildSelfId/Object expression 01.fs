//${RUN:1}

[<Interface>]
type A =
    abstract member Get: unit -> unit
    abstract member Set: unit -> unit

{
    new A with 
        member __{caret}.Get() = ()    
        member __.Set() = ()
}
