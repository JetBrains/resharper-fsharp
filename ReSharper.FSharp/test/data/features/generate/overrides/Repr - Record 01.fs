// ${KIND:Overrides}
// ${SELECT0:ToString():System.String}
module A

let moreThanOne<'a> : 'a list -> bool =
    function
    | []
    | [ _ ] -> false
    | _ -> true

type R =  {
        I: int
} {caret}

type U = 
    | S of int

    override this.ToString() = failwith "todo"
