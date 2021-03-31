module Module

let _: Enum = enum 0
let _: Class = enum 0

let _: EnumShort = enum 0
let _: EnumShort = enum 0s

let _: EmptyEnum = enum 0
let _: EnumWrongBaseType = enum 0


System.Nullable<Enum>() |> ignore
System.Nullable<Class>() |> ignore


match Enum.A with
| Enum.A -> ()
| Enum.B -> ()
| Enum.C -> ()
| Enum.D -> ()

match EnumShort.A with
| EnumShort.A -> ()
| EnumShort.B -> ()

match EnumWrongBaseType.A with
| EnumWrongBaseType.A -> ()
| EnumWrongBaseType.B -> ()

match EnumWrongValueType.A with
| EnumWrongValueType.A -> ()

match EnumOverflowValue.A with
| EnumOverflowValue.A -> ()
| EnumOverflowValue.B -> ()

match EnumSameFields.A with
| EnumSameFields.A -> ()

match EnumSameFields.A with
| EnumSameFields.A -> ()
| EnumSameFields.A -> ()
