@echo off
rmdir /S /Q blotch3d
nuget init ../src/bin/release .
nuget setApiKey oy2ozsgwazdcxhvcilqaqg7oj4ekybcu5lwkkotc6vfcdu
echo.
echo.
echo %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
echo NOTICE:
echo Comment-in the following line to push to nuget:
nuget push ../src/bin/release/Blotch3D.1.0.7.nupkg -Source https://api.nuget.org/v3/index.json
pause
