// ${COMPLETE_ITEM:override ToString()}
module Foo

type C () =
    class
        member val Foo: int = 99
        {caret}
    end
