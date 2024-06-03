// lang=json
let s1 = "{ \"key\": \ \ж \r\n \xD \u000D \U0000000D \"value\" }"

// lang=json
let s2 = $"{{ \"key\": \r\n \xD \u000D \U0000000D \"{args[0]}\" }}"

// lang=json
let s3 = @"{ ""key"": ""{args[0]}"" }"

// lang=json
let s4 = $@"{{ ""key"": ""{args[0]}"" }}"
