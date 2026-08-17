// ${ABSENT_ITEM:Foo}
module Module

type Base() =
    abstract Foo: unit -> unit
    default this.Foo() = ()

type A() =
    inherit Base()

    override this.Foo() = ()
    override this.{caret}
