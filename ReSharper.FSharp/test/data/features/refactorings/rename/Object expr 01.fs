type T() =
    abstract P: int
    default val P = 1

{ new T() with
    override x.P = base.P }

{ new T() with
    override x.P{caret} = base.P }
