del *.dll
del *.exe
csc.exe /platform:x86 /target:library /out:spdrw.dll /o SpdReaderWriterDll\*.cs SpdReaderWriterDll\SPD\*.cs SpdReaderWriterDll\Driver\*.cs
csc.exe /platform:x86 /target:exe /reference:spdrw.dll /out:spdrwcli.exe /o SpdReaderWriter\*.cs
