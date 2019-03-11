# Release notes

## 2019.1

### Refactorings

* Rename for F#-defined symbols

### Navigation and cross-language interop

* Go to next/previous/containing member actions now work for top level `let` bindings
* Global navigation and Go to File Member action now work for type-private `let` bindings
* F# defined delegates are now supported and are properly seen by code in other languages
* Intrinsic type extensions are now considered type parts with navigation working for every part
* Navigation to symbols with escaped name now places caret inside escaped identifiers

### Find usages

* Usages inside `let` bindings are properly grouped by containing members
* Separate grouping for new occurrence kinds:
	* Type argument
	* Base type
	* Type checking
	* Type conversions
	* Module or namespace import
	* Write
	* Attribute reference
* Highlight Usages in File action now highlights Write occurrences differently to other occurrences

### Completion

* An option for out-of-scope completion to place `open` statements to the top level was added and enabled by default by [@saul](github.com/saul)
* Out-of-scope completion can be disabled now

### Code analysis

* Format specifiers are now highlighted, by [@vasily-kirichenko](github.com/vasily-kirichenko)
* Unused local values analysis is now turned on for all projects and scripts by default
* Debugger now shows local symbols values next to their uses

### Misc

* Notification about F# projects having wrong project type guid in solution file
* F# Interactive in FSharp.Compiler.Tools is now automatically found in non-SDK projects
* FSharp.Compiler.Interactive.Settings.dll is now bundled to the plugin (so `fsi` object is available in scripts)
* F# breadcrumbs provider was added

### Fixes

* Pair paren was not inserted in attributes list
* Tab left action (Shift+Tab) didn't work
* Tests defined as `let` bindings in types were not found
* Incremental lexers were not reused, by [@misonijnik](github.com/misonijnik)
* Type hierarchy action icon was shown in wrong places
* Usages of operators using their compiled names were highlighted incorrectly
* Local rename didn't work properly for symbols defined in Or patterns
* Better highlighting of active pattern declarations
* Extend Selection works better for patterns and `match` expressions
* Resolve and navigation didn't work for optional extension members having the same name in a single module
* Secondary constructors resolve was broken in other languages
