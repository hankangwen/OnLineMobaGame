@echo off

if exist gen ( rmdir /s /q gen )
md gen
for %%p in (source\*.proto) do (
	echo Դ�ļ���%%p 
	protoc.exe --csharp_out=.\gen  %%p 
)
echo ������ϣ�
pause
