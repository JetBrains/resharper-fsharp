// ${KIND:Overrides}
// ${SELECT0:ToString():System.String}

// ${KIND:Overrides}

type C(y: int) = class{caret}
                    // comment 1
                    let x = y // comment 2

                 end
                 // comment 3
                 override this.GetHashCode() = failwith "todo"
