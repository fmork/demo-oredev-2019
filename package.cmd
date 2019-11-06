@echo off

dotnet lambda package --configuration release --framework netcoreapp2.1 --project-location src/demunity
if %ERRORLEVEL% NEQ 0 goto :error

dotnet lambda package --configuration release --framework netcoreapp2.1 --project-location src/demunity.imagesizer
if %ERRORLEVEL% NEQ 0 goto :error

dotnet lambda package --configuration release --framework netcoreapp2.1 --project-location src/demunity.popscore
if %ERRORLEVEL% NEQ 0 goto :error



goto :eof

:error
echo Something went wrong, there were errors.
