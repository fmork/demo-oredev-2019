@echo off

dotnet test src/demunity.lib.tests --configuration release --logger:console;verbosity=normal
if %ERRORLEVEL% NEQ 0 goto :error

dotnet test src/demunity.tests --configuration release --logger:console;verbosity=normal
if %ERRORLEVEL% NEQ 0 goto :error

dotnet test src/demunity.aws.tests --configuration release --logger:console;verbosity=normal
if %ERRORLEVEL% NEQ 0 goto :error

goto :eof

:error
echo Something went wrong, there were errors.
