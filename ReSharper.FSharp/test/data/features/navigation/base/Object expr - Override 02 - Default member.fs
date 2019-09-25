type T() =
    abstract P: int
    default x.P = 1

{ new T() with
      override x.P{on} = 1 }
