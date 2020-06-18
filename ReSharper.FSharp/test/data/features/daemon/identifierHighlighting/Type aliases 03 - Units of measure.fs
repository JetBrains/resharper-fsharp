[<Measure>] type m

type IntMeasure = int<m>

type MeasureClass<[<Measure>] 'a>() = class end
let mclsctor = MeasureClass<m>
let mcls = MeasureClass<m>()