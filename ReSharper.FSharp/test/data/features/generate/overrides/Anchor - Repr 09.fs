// ${KIND:Overrides}
// ${SELECT0:ToString():System.String?}

type R = { F: int }{caret}
         override this.GetHashCode() = failwith "todo"

         member this.P = [ 1
                           2 ]