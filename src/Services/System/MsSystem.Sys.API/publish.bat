@echo off
:: ������ֵ��ʹ��!name!

setlocal enabledelayedexpansion

set currentPath=%~dp0
set tempModulesPath=%currentPath%\temp
set modulesPath=%currentPath%\src\UI\

set str="publish"

:GOON
for /f "delims=,+, tokens=1,*" %%i in (%str%) do (
    echo --------------------------------------------------------
    echo ��%%i��������ʼ
    set path1=%modulesPath%%%i
    set path2=%currentPath%\release\%%i\
    set filnePath=!path2!app_offline.htm
    echo !path1!
    echo ֹͣ��%%i��վ��
    if not exist !path2! md !path2!

    cd /d !path1!
    echo ִ�з�����!path2!��
    echo ��վά����>!filnePath!
    call dotnet publish -o !path2!
    call xcopy %tempModulesPath% !path2! /s /e /Q /Y /I
    del !filnePath!
    echo ������%%i��վ��
    
    echo ��%%i���������
    set str="%%j"
    goto GOON
)

pause