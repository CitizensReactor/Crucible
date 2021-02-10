# Crucible
Crucible is a lightweight and high-performance filesystem management tool for Star Citizen that provides a single user interface experience for working with all of the files inside of a P4K file and the local filesystem. The goal of Crucible is to provide a foundation to share code that all plugins require universally such as filesystem, database, versioning, and encryption.

### Crucible Core Principles
* Performance is the most important tool of all and with high core count processors taking advantage of that horsepower is key.
* Crucible is a mixture of C# and C++ using the C++/CLI framework.
* Almost everything is a plugin.
    * Allows us to support filetypes across different versions of the game using separate plugins.
    * Allows multiple plugins to support the same file.
    * Lots of smaller projects that many developers can work on rather than a single mega tool.

## Social Channels
Discord: https://discord.gg/Ef6Euh5
Reddit: https://www.reddit.com/r/CitizensReactor

## Crucible Plugin Guidelines
* Distributed plugins must be a single binary that is placed in the `plugins` directory of Crucible. (Take a look at Fody Costura)
* Distributed plugins may contain extra dependencies and external files which must be stored in a resources folder called `<plugin>_Resources`.
* If your plugin sucks and you don't provide any kind of support for it, it will be removed from the community.
* Do not thread block the call back made to `RegisterFileUI` for extended periods of time. You should implement asynchronous multithread code and provide loading UI for your plugin.
* Do not crash Crucible ever. Badly behaving plugins that do not catch their errors will potentially be disabled in future releases
* Do not embed binaries that conflict with Crucible's binaries.
* All plugins are considered derivative works and must share a compatible licence.
