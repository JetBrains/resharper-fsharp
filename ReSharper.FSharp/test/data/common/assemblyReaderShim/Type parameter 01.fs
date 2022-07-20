module Module

let _: int = Class1<int>.StaticField


let _: Class2<int, int64, float> = Class2<int, int64, float>.StaticField

let _: Class2<int64, int, float>.Nested<bool, string> =
    Class2<int64, int, float>.Nested<bool, string>.StaticField


let _: obj = Class4<string, float>.StaticField


type T1 = Class2<int, int64, float>
type T2 = Class2<int64, int, float>.Nested<bool, string>
type T4 = Class4<int, double>

let _: int = T1.Field1
let _: int64 = T1.Field2
let _: float = T1.Field3

let _: int64 = T2.NestedField1
let _: int = T2.NestedField2
let _: float = T2.NestedField3
let _: bool = T2.NestedField4
let _: string = T2.NestedField5

let _: obj = T4.StaticField


let _: float = Ns1.NsClass1<float>.StaticField

let _: Ns1.Ns2.NsClass2<int, float> =
    Ns1.Ns2.NsClass2<int, float>.StaticField

let _: Ns1.Ns2.NsClass2<int, uint>.Nested<int64, uint64, bool> =
    Ns1.Ns2.NsClass2<int, uint>.Nested<int64, uint64, bool>.StaticField


let _: Class3<int8, uint8>.Nested1<int16, uint16>.Nested2<int32, uint32>.Nested3<int64, uint64> =
    Class3<int8, uint8>.Nested1<int16, uint16>.Nested2<int32, uint32>.Nested3<int64, uint64>.StaticField


type T() =
    inherit Class4<bool, unit>()

    override this.Property = true
