module CsvTypeProvider

open FSharp.Data
open FSharp.Data.UnitSystems.SI.UnitNames

type CsvUom = CsvProvider<"
Name, Distance (metre),Time (s)
First, 50.0,3.7
">

let row = new CsvUom.Row("name", 3.5M<metre>, 27M<Data.UnitSystems.SI.UnitSymbols.s>)
let rowError1 = new CsvUom.Row("name", 3.5M<metre>, 27M<metre>)
let rowError2 = new CsvUom.Row("name", 3.5M<metre>, 27M)

let m: decimal<meter> = row.Distance
let mError1: decimal<meter> = row.Time
let mError2: decimal = row.Time
