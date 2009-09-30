@echo off

SET DIR=%~d0%~p0%

::SET file.settings="%DIR%..\..\settings\${environment}.settings"

::"%DIR%NAnt\nant.exe" /f:"%DIR%scripts\database.deploy" -D:file.settings=%file.settings%
"%DIR%NAnt\nant.exe" /f:"%DIR%scripts\database.deploy" -D:dirs.db.project=db -D:server.name=(local) -D:database.name=TestRoundhousE