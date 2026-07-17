#!/usr/bin/env bash

set -euo pipefail

cd build

rm -rf Branchline.app
mkdir -p Branchline.app/Contents/Resources
mv Branchline Branchline.app/Contents/MacOS
cp resources/app/Branchline.icns Branchline.app/Contents/Resources/Branchline.icns
sed "s/SOURCE_GIT_VERSION/$VERSION/g" resources/app/App.plist > Branchline.app/Contents/Info.plist
rm -rf Branchline.app/Contents/MacOS/SourceGit.dsym
rm -f Branchline.app/Contents/MacOS/*.pdb
codesign --force --deep --sign - Branchline.app

ditto -c -k --sequesterRsrc --keepParent Branchline.app "branchline_$VERSION.$RUNTIME.zip"
