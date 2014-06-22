This is a "Shared Project", a new project type introduced by Microsoft in April 2014.

It requires Visual Studio 2013 Update 2 and the Shared Projects extension,
http://visualstudiogallery.msdn.microsoft.com/315c13a7-2787-4f57-bdf7-adae6ed54450

Xamarin Studio supports it as of v.5.0.1: http://developer.xamarin.com/guides/cross-platform/application_fundamentals/shared_projects/

XAMARIN STUDIO BUG:

At this writing, Xamarin Studio has difficulty building the target device project,
displaying an error such as "The type or namespace 'Models' does not exist in the namespace 'Todo'".

It's intellisense understands "Todo.Models" perfectly well. This is clearly a Xamarin Studio bug. 

Rebuilding in Visual Studio clears the problem and you then can deploy to an emulator or device.

ALTERNATIVES:

An alternative, old school approach is to manually link to these files from the device-specific projects.
See http://support.microsoft.com/kb/306234