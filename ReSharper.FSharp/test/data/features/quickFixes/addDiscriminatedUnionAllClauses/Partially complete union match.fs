module Say
type TestUnion =
    | FieldlessCase
    | CaseWithSingleField of int
    | CaseWithMultipleFields of string * int

// Not available if the match statement is partially complete
let unionCaseMatch = TestUnion.FieldlessCase
match unionCaseMatch with
| FieldlessCase -> failwith ""