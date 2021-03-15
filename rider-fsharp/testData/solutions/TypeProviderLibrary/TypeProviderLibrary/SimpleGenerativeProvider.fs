module TypeProviderLibrary.SimpleGenerativeProvider

open SimpleGenerativeProviderNamespace

type S = SimpleGenerativeType
type S1 = S.GenerativeSubtype

let property1: string = S.GenerativeSubtype.Property
let property2: string = S1.Property
