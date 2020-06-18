[<Measure>] type m

type IntMeasure = int<m>

type MeasureClass<[<Measure>] 'a>() = class end

let _: MeasureClass<m> = MeasureClass<m>()
