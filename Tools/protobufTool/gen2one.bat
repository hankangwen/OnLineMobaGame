rem 导出所有的proto到同一份cs文件中
@echo off
setlocal enabledelayedexpansion

set "myString=protogen.exe"
for %%i in (proto\*.proto) do (
    set "anotherString=-i:%%i"
    set "myString=!myString! !anotherString!"
)
set "anotherString2=-o:cs\PBMessage.cs -ns:PBMessage"
set "myString=!myString! !anotherString2!"
echo %myString%
call %myString%
pause

rem cd ..\
rem protobuf-net\ProtoGen\protogen.exe ^
rem -i:login.proto ^
rem -i:register.proto ^
rem -o:PBMessage\PBMessage.cs -ns:PBMessage
rem cd gen

