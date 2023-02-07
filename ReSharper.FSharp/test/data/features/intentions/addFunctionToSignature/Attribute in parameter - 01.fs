module Test

open System.Diagnostics.CodeAnalysis
let x{caret} ([<NotNull>] y) = y + 0
