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
Discord: https://discord.gg/3Hq4uM7
Reddit: https://www.reddit.com/r/CitizensReactor

## Crucible Plugin Guidelines
* If you have to ever ask the question "should I post this?" then don't.
* Distributed plugins must be a single binary that is placed in the `plugins` directory of Crucible. (Take a look at Fody Costura)
* Distributed plugins may contain extra dependencies and external files which must be stored in a resources folder called `<plugin>_Resources`.
* If your plugin sucks and you don't provide any kind of support for it, it will be removed from the community.
* Do not thread block the call back made to `RegisterFileUI` for extended periods of time. You should implement asynchronous multithread code and provide loading UI for your plugin.
* Do not write tutorials on making plugins, if people aren't smart enough to work this stuff out for themselves, they shouldn't be developing plugins.
* Do not crash Crucible ever. Badly behaving plugins that do not catch their errors will be disabled automatically in future releases and kindly shaming you with a popup message. 
* Do not embed binaries that conflict with Crucible's binaries. If you're doing this, you're just doing things wrong you should stop and just try again.
* All plugins are considered derivative works and must share a compatible licence.

## Crucible Official Plugins
A slightly modified version of Crucible will be available for public download from a continuous integration. All official plugins will be packaged with Crucible right out of the box. Official plugins are to be signed and verifiable using the official plugins. In the future, users will have to manually allow third party plugins.

* Official plugin signing will be a thing in the near future.
* Plugins that suck won't become official.
* Support the heck out of your plugin.
* Write extremely good and documented code. *(Crucible is not an example of good code rip)*
* Pro tip: Don't let the community write tutorials for your plugins as they don't maintain them, or they just put weird stuff in there that confused people and usually does nothing. *Do it yourself.*
* Official plugins may be allowed to merge their code into Crucible repository to be maintained by core developers.

### Criteria for sucking
 * The plugin crashes, people knew about it, but nobody fixed it for ages.
 * Compiling the plugin on the CI was broken and was not fixed before an official release.
 * Untrustworthy developers or generally shit people.
