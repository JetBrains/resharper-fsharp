module Module

open T
let t1 = T()
let t2: T.NestedModuleType = T.NestedModuleType()
let t3: T.AnotherNestedModuleType = T.AnotherNestedModuleType()
let v1 = T.moduleValue
let v2 = T.AnotherNestedModuleType.nestedModuleValue

open GenericT
let v3 = GenericT.genericModuleValue

open T.NestedModuleType
let t4 = T.NestedModuleType()
let v4 = T.NestedModuleType.nestedModuleValue
