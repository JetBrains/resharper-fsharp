[<Measure>] type m

type MeasureRecord<[<Measure>] 'u> = { x : float<'u> }
let mrec : MeasureRecord<m> = { x = 0.0<m>; }

type MeasureDiscriminatedUnion<[<Measure>] 'a> = | IntMeasureMember of decimal<'a>
let mdu = MeasureDiscriminatedUnion<m>.IntMeasureMember 10m<m>

type MeasureClass<[<Measure>] 'a>() = class end    
let mcls = MeasureClass<m>()

[<Struct>]
type MeasureStruct<[<Measure>] 'a>(__ : int<'a>) = struct end
let mstr = MeasureStruct<m>()