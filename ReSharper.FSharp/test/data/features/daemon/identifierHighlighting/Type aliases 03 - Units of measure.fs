[<Measure>] type m
[<Measure>] type s

type MeasureBasicType = int<m/s>

type MeasureRecord<[<Measure>] 'u> = { x : float<'u>; y : float<'u>; z : float<'u> }
let mrec : MeasureRecord<m> = { x = 0.0<m>; y = 0.0<m>; z = 0.0<m> }

type MeasureDiscriminatedUnion<[<Measure>] 'a> =
    | IntMeasureMember of int<'a>
    | FloatMeasureMember of float<'a>
let mdu = MeasureDiscriminatedUnion<s>.IntMeasureMember 10<s>

type MeasureClass<[<Measure>] 'a>(value : int<'a>) =
    member x.Value = value
let mcls = MeasureClass<s> 10<s>

[<Struct>]
type MeasureStruct<[<Measure>] 'a>(value : int<'a>) =
    member x.Value = value    
let mstr = MeasureStruct<s> 10<s>