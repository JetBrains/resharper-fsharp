open System.Text

type U =
    | A of someFieldName: bool * int * StringBuilder * anotherField: double

match A (true, 2, null, 4.) with
| _{caret} -> failwith "todo"
