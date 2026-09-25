// ${COMPLETE_ITEM:ToString()}
module Module

type A() =
    override x.{caret} : int = failwith "preserved"
    member _.Bar = 1
