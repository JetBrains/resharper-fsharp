(* language=json *)
let s1 = $"""
         [ {{
              "key": {args[0]}
           }} ]
         """

let s2 = (*language=xml*) $"""
                          <tag1>
                            <tag2 attr="{0}"/>
                          </tag1>
                          """

// language=css
let s3 = $""".my-awesome-class {}"""

(* language=js *)
let s4 = $"""alert("Hello")