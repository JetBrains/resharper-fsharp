module Module

type T() =
    inherit System.Object()

    override this.ToString() =
        let ``base`` = 1
        {caret}
        failwith "todo"
