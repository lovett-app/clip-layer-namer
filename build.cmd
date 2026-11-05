@echo off
chcp 65001 >nul
rem 윈도우에 들어 있는 C# 컴파일러로 "레이어 이름 붙이기.exe" 를 만듭니다. (아이콘은 icon.ico)
cd /d "%~dp0"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /codepage:65001 /target:winexe /optimize+ /out:"레이어 이름 붙이기.exe" /win32icon:icon.ico /resource:icon.ico,icon.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll src\*.cs
if errorlevel 1 pause
