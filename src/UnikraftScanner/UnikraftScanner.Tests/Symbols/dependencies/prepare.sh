#!/bin/bash
cd ../../UnikraftScanner.Client/Symbols
make
cp ./bin/*.so ../../UnikraftScanner.Tests/Symbols/dependencies/TestPluginBlockFinder.so
# go back to where PrePareSymbolMain.cs is
cd -