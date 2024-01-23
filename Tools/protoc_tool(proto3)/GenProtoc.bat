@echo off

if exist gen ( rmdir /s /q gen )
md gen
for %%p in (source\*.proto) do (
	echo 源文件：%%p 
	protoc.exe --csharp_out=.\gen  %%p 
)
echo 导出完毕！
pause
