call package.cmd
@if %ERRORLEVEL% NEQ 0 goto :error

serverless deploy --stage dev --aws-profile demo-developer

@goto :eof

:error
@echo Something went wrong, there were errors.

