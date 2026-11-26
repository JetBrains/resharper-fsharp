module Module

open Expecto

let tests0 = testCase "test" <| fun _ -> ()
let tests1 = testList "1" [ tests0; tests0 ]
let tests2 = testList "2" [ tests1; tests1 ]
let tests3 = testList "3" [ tests2; tests2 ]
let tests4 = testList "4" [ tests3; tests3 ]
let tests5 = testList "5" [ tests4; tests4 ]
let tests6 = testList "6" [ tests5; tests5 ]
let tests7 = testList "7" [ tests6; tests6 ]
let tests8 = testList "8" [ tests7; tests7 ]
let tests9 = testList "9" [ tests8; tests8 ]
let tests10 = testList "10" [ tests9; tests9 ]
let tests11 = testList "11" [ tests10; tests10 ]
let tests12 = testList "12" [ tests11; tests11 ]
let tests13 = testList "13" [ tests12; tests12 ]
let tests14 = testList "14" [ tests13; tests13 ]
let tests15 = testList "15" [ tests14; tests14 ]
let tests16 = testList "16" [ tests15; tests15 ]
let tests17 = testList "17" [ tests16; tests16 ]
let tests18 = testList "18" [ tests17; tests17 ]

[<Tests>]
let allTests = testList "All" [ tests18; tests18 ]
