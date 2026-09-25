// ${COMPLETE_ITEM:ToString()}
module Module

type A() =
    member _.Bar = 1
    override x.{caret} : int = failwith "preserved"
