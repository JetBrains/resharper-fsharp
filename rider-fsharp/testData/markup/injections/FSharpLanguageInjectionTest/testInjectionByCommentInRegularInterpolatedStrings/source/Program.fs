// language=json
var s1 = $"[ ]"

// language=json
var s2 = $"[ {{ \"key\": \"{42}\" }} ]"

var s3 = (*language=xml*) $"<tag1><tag2 attr=\"{s1}\"/></tag1>"

// language=sql
var s4 = $" {5} select *"

// language=css
var s5 = $".unfinished {
