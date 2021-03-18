module TypeProviderLibrary.SimpleGenerativeProvider

open SimpleGenerativeProviderNamespace

type S = SimpleGenerativeType
let property1: string = S.GenerativeSubtype.Property

type S1 = S.GenerativeSubtype
let property2: string = S1.Property
