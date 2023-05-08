// lang=json prefix={ postfix=}
let s1 = "\"key\": \"value\"";

let s2 = (*lang=xml*) $@"<tag1><tag2 attr=""{args[0]}""/></tag1>"

//lang=css
let s3 = $"""
         .my-class {{
           font-size: {args[0]}
         }}
         """
