// language=json
let s1 = $@"[ ]"

// language=json
let s2 = @$"[ {{
    ""key"": ""{42}""
    }} ]"

let s3 = (*language=xml*) $@"<tag1><tag2 attr=""{s1}""/></tag1>"

// language=css
let s3 = $@".unfinished
            {{