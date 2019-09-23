module Module

type Foo() = class end

type IInterface =
    abstract Prop: Foo{caret}

type T() =
    interface IInterface with
        member x.Prop = Foo()

let i: IInterface = Unchecked.defaultof<_>
i.Prop
