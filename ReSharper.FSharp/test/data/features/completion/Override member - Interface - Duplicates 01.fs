// ${ABSENT_ITEM:Dispose()}
module Module

type A() =
    interface System.IDisposable with
        member this.Dispose() = failwith "todo"
        member this.{caret}
