// ${KIND:Overrides}
// ${SELECT0:ToString():System.String?}

type C() = class{caret}
  member this.X = [ 1
                    // comment 1
                    2 // comment 2
                    3 ]
end
