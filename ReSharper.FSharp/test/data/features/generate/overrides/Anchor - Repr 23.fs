// ${KIND:Overrides}
// ${SELECT0:ToString():System.String}

// ${KIND:Overrides}

module M

type C(y: int) = class

        do
            let f{caret} x y = x + y
            ()

        let x = y

    end
