open System.Text.RegularExpressions

let x = 5

let _ = Regex("[123] \\b")
let _ = Regex($"[123] \\b")
let _ = Regex(@"[123] \\b")
let _ = Regex(@"[123] \b")
let _ = Regex(@$"[123] \b")
let _ = Regex("""[123] \b""")
let _ = Regex($"""[123] \b""")
let _ = Regex($$"""[123] \b""")

let _ = Regex($"[123] {x} \\b")
let _ = Regex(@$"[123] {x} \b")
let _ = Regex($"""[123] {x} \b""")
let _ = Regex($$"""[123] {{x}} \b""")
