del *.dll
del *.exe
csc.exe /platform:x86 /target:library /out:spdrwcore.dll /o SpdReaderWriterCore\*.cs SpdReaderWriterCore\SPD\*.cs SpdReaderWriterCore\Driver\*.cs
csc.exe /platform:x86 /target:exe /reference:spdrwcore.dll /out:spdrwcli.exe /o SpdReaderWriter\*.cs
