dotnet build && (
    cp bin/Debug/net48/TootFloot.dll /media/d/SteamLibrary/steamapps/common/REPO/BepInEx/plugins/;
    rm TootFloot.zip;
    cp bin/Debug/net48/TootFloot.dll .
    zip TootFloot.zip README.md manifest.json TootFloot.dll icon.png
    rm TootFloot.dll
)