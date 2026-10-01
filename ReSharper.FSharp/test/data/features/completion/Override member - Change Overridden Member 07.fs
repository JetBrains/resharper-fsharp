// ${COMPLETE_ITEM:ToString()}
// ${COMPLETION_TYPE:Insert}
module Module

type A() =
    member _.Bar = 1
    override x.{caret}Foo : int = failwith "preserved"
