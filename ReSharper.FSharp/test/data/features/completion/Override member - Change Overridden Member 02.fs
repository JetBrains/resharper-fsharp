// ${COMPLETE_ITEM:ToString()}
module Module

type A() =
    override x.{caret} () : int =
        let a = 1
        let b = 2
        failwith "preserved"
    member _.Bar = 1
