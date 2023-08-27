@echo off

webmerge -f "%~dp0\.optimize.conf.xml" --optimize --jobs 32 -l 6 %*

pause