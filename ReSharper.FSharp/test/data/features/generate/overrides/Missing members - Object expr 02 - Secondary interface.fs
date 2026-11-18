// ${KIND:Implementations}
// ${SELECT0:Dispose():System.Void}

let o =
    { new obj() with
        override this.ToString() = ""

      interface System.IDisposable{caret} }
