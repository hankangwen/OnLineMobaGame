@echo off
for %%i in (proto\*.proto) do (
	protogen.exe -i:%%i -o:cs\%%~ni.cs -ns:PBMessage
)
pause

rem 旧的 -ns:PBMessage修改默认导出命名空间
rem protogen.exe -i:proto\MsgPing.proto -o:cs\MsgPing.cs -ns:PBMessage
rem protogen.exe -i:proto\MsgPong.proto -o:cs\MsgPong.cs -ns:PBMessage
rem pause