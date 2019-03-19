namespace MyNamespace

module R =
    let x = 123

type R = { Field: int }


module U =
    let x = 123

type U =
    | CaseA


module O =
    let x = 123

type O() =
    class
    end


module E =
    let x = 123

type E =
    | Case1 = 1
