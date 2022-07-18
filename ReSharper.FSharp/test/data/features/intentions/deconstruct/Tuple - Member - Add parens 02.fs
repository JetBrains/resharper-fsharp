type I<'T> =
    abstract M: 'T -> unit

type T1() =
    interface I<int * int> with
        member this.M t{caret} = failwith "todo"
