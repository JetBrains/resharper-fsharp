// ${COMPLETE_ITEM:override ToString()}
module Module

type C () =
    class
        member val Foo: int = 99
        {caret}
    end
