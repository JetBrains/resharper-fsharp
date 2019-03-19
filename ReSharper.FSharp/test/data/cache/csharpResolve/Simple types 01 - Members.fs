module Module

type R =
    { Foo: int }
    
    member x.Prop = 123
    member x.VoidMethod() = ()
    member x.Method() = 123
    member x.Method(123) = 123
    static member StaticMethod() = 123
    static member StaticProp = 123

type U =
    | Case
    member x.Prop = 123
    member x.VoidMethod() = ()
    member x.Method() = 123
    member x.Method(123) = 123
    static member StaticMethod() = 123
    static member StaticProp = 123

exception E with
    member x.Prop = 123
    member x.VoidMethod() = ()
    member x.Method() = 123
    member x.Method(123) = 123
    static member StaticMethod() = 123
    static member StaticProp = 123
