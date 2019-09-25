type T() =
    override x.ToString() = ""

{ new T() with
      override x.ToString{on}() = "" }
