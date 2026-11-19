module Module

let x = 1
let regular = "wrongword"
let verbatim = @"wrongword"
let tripleQuote = """wrongword"""
let interpolated = $"wrongword {x} wrongword"
let verbatimInterpolated = $@"wrongword {x} wrongword"
let tripleQuoteInterpolated = $"""wrongword {x} wrongword"""
let bytes = "wrongword"B
let escaped = "\twrongword"
let escapedLines = "hello\nwrongword\rhello"
let verbatimQuote = @"a""wrongword"
let interpolatedBraces = $"{{wrongword}} {x}"
let continuation = "hello \
    wrongword"
