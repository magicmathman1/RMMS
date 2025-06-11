# RMMS
RainMeadow Mod Sorter is a utility program designed to allow for easy sorting of the RainMeadow lobby allowed, banned, and high-impact mods, written in C#. This version of RMMS uses a text-based UI. Don't worry, I took time making it look decent!

dotnet publish -c Release -r linux-x64 --self-contained true -o ./publish
+----------------------------+

**Compile Instructions**
1. Install Dotnet if you haven't already. [.net Download (Microsoft)](https://dotnet.microsoft.com/en-us/download)
2. In the project's root directory in your favorite terminal, run
     - **For Windows**: `dotnet publish -c Release -r win-x64 --self-contained true -o ./publish`
     - **For Linux**: `dotnet publish -c Release -r linux-x64 --self-contained true -o ./publish`
     - *Remove --self-contained true if you keep dotnet installed because it bloats the folder size.*
     - *Make sure you have dotnet added to your Path, otherwise use the dotnet binary path.*
3. Congratulations, you've stumbled across a project so simple, it doesn't require any stupid additional libraries to bloat your drive!

+----------------------------+

**Some History**

Initially, this program was going to be written in [Lua](https://lua.org), but after trouble with getting [LuaRocks](https://luarocks.org/) to work with [LuaJIT](https://luajit.org/), it was decided that this project would be written in C# instead. A good friend of mine helped me understand all the requirements of a mod sorter for Rain Meadow, as I am not as familiar with the mod. I ended up writing this in two days, enjoying my summer break. This program was actually my first ever program written in C# (I litterally mean first- this was before a _Hello World!_), so I definitely am more proud of it than I should be.

+----------------------------+

This project is completely open-source under the GNU General Public License v3, allowing you to  
- Use this software,  
- Modify this software,  
- Redistribute this software,

free of charge, with the sole condition that all modified and/or redistributed versions utilize the same license.(See LICENSE for license.)

Credit is not required, but appreciated!
