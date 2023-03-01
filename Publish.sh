#!/bin/sh
dotnet build -c Release
rm -rf release_me CollabLobbyUI.zip
mkdir release_me
cp everest.pub.yaml release_me/everest.yaml
cp -r bin/Release/net452/* release_me
cp -r Dialog release_me
(cd release_me/ && 7z a -tzip -r ../CollabLobbyUI.zip ./*)
