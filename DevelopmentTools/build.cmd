CD %~dp0
CD ..

CALL DevelopmentTools\DevScripts\Commands\app.cmd dotnet standard ..\UniTool UniTool

IF "%1"=="release" GOTO release
GOTO end

:release
CALL DevelopmentTools\DevScripts\Commands\app.cmd dotnet release ..\UniTool UniTool %2 %2

:end
