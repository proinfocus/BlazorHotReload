# Blazor Hot Reload
A simple solution for hot reload of Blazor apps.

## Steps to use
1. Download the **HotReload.zip** file.
2. Extract it to a folder for eg: `C:\HotReload`
3. Set Path in the Environment Variables, preferrably System variables to this folder.
4. Copy `hotreload.js` file from the extracted folder into your Blazor project's `wwwroot` folder.
5. Add this script below in the `index.html` file `<script src='hotreload.js'></script>`
6. Go to your project's folder in Command Line or Terminal.
7. Type `hr .\yourproject.csproj c:\folderToMonitor`
8. Open the browser and start making changes.
9. Do not run your project from Visual Studio or VS Code.
10. Just make changes and have fun!
   
## Requirements
You need .NET 8 runtime installed on your PC.
