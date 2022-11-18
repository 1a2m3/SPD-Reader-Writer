del *.dll
del *.exe
csc.exe /warn:0 /platform:x86 /target:library /out:spdrw.dll /o SpdReaderWriterDll\*.cs SpdReaderWriterDll\SPD\*.cs
csc.exe /warn:0 /platform:x86 /target:exe /reference:spdrw.dll /out:spdrwcli.exe /o SpdReaderWriter\*.cs
