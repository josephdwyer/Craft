# Craft

Craft is a simple text editor intended to be used for creating high quality git commit messages (with emoji). If you are using the [Conventional Commits standard](https://www.conventionalcommits.org) you can use the emoji aliasing feature to map a type to an emoji e.g. 💊 for bug, ✨ for feature etc.

## Features
* Starts you off right, Craft will automatically start the commit message with the last segment of the git branch you are committing to e.g. `joe/win-100` would start you out with `WIN-100:`
* Emoji support, use emoji one codes directly (`:arrow_down:`)
* Emoji autocomplete, use a configuration file to give more succinct or meaningful search text for emoji you use often. 
* Get back to work, when you are done crafting you can use `ctrl+s` or the `Done` button to save and close the editor.

## Development
Craft uses a simple prototype reactive framework called Curfew. In a nutshell, there is a global dispatcher and view specific dispatchers (analogous to a view model). Information that needs to be passed from one area of the app to another should sent on the global dispatcher and information that is view specific should be sent on the view dispatcher. All messages and state objects should be immutable, as such if a view receive several of the same kind of message (because it paused notifications) it often makes sense to only process the last message. 

1. Open and compile `Craft.sln`  
1. Copying the contents of the `bin` folder to some known location so your current git branch won't mess with your commit editor e.g. `C:\code\Craft`  
1. Configure git to use this editor for commit messages `git config --global core.editor 'C:/code/Craft/Craft.exe'` (you could also manually edit `~/.gitconfig`

Most of the code is in `MainWindow.xaml.cs` and `MainWindowViewDispatcher`. We use a single instance mutex which will signal a `LoadFileOperation` if another instance tries to open, this makes it easy to debug as you can start an instance from visual studio and then trigger a git commit and step through all of the startup code.
