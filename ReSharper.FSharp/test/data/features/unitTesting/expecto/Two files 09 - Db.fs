module Db

open Expecto

let tests =
    testList "Db" [
        testCaseAsync "query" (async { () })
    ]
