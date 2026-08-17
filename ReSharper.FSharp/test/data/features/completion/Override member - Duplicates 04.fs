// ${ABSENT_ITEM:Foo}
module Module

type Base() =
    abstract Foo: int with get, set
    default this.Foo = 1
    default this.Foo with set v = ()

type A() =
    inherit Base()

    override this.Foo with set v = ()
    override this.Foo = 1

    override this.{caret}
