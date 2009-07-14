@ECHO OFF
:: Original script by wj32.
:: Modifications and additions by XhmikosR and Yz�wl.
SETLOCAL
SET outd=%~p1
PUSHD %outd%

:: Copy CHANGELOG.txt, LICENSE.txt, README.txt and kprocesshacker.sys to the
:: "Release" folder
FOR %%a IN (
	"CHANGELOG.txt" "LICENSE.txt" "README.txt"
    "KProcessHacker\i386\kprocesshacker.sys" 
    "NProcessHacker\Release\NProcessHacker.dll"
	) DO COPY "..\..\..\%%a" >NUL

:: Clear older files present in "Release" folder
DEL/f/a "ProcessHacker.exe.config" "processhacker-*-setup.exe"^
 "Assistant.dll" "processhacker-*.zip" >NUL 2>&1

:: Check if ILMerge is present in the default installation location or in PATH
SET ILMergePath="%PROGRAMFILES%\Microsoft\ILMerge\ILMerge.exe"
IF NOT EXIST %ILMergePath% (FOR %%a IN (ILMerge.exe) DO IF %%~$PATH:a' NEQ ' (
		SET ILMergePath="%%~$PATH:a") ELSE (SET "N_=T"
			ECHO:ILMerge IS NOT INSTALLED!!!&&(GOTO CLEANUP)))

SET RequiredDLLs="Aga.Controls.dll" "ProcessHacker.Common.dll"^
 "ProcessHacker.Native.dll"

:: Create a temporary directory for the merged files
mkdir tmp

:: Merge DLLs with "Assistant.exe"

%ILMergePath% /t:exe /out:"tmp\Assistant.exe" "Assistant.exe"^
 %RequiredDLLs% && ECHO:DLLs merged successfully with Assistant.exe!

:: Merge DLLs with "ProcessHacker.exe" using ILMerge
%ILMergePath% /t:winexe /out:"tmp\ProcessHacker.exe" "ProcessHacker.exe"^
 %RequiredDLLs% && ECHO:DLLs merged successfully with ProcessHacker.exe!

:: Delete the existing EXEs and PDBs
del ProcessHacker.exe Assistant.exe *.pdb
:: Copy the merged files (two EXEs and two PDBs) back into this directory
move tmp\* .\

DEL/f/a %RequiredDLLs% "ProcessHacker.Common.xml" "ProcessHacker.Native.xml" >NUL 2>&1

:: Set the path of Inno Setup and compile installer
SET "U_=HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
SET "I_=Inno Setup"
SET "A_=%I_% 5"
SET "M_=Inno Setup IS NOT INSTALLED!!!"
FOR /f "delims=" %%a IN (
	'REG QUERY "%U_%\%A_%_is1" /v "%I_%: App Path"2^>Nul^|FIND "REG_"') DO (
	SET "InnoSetupPath=%%a"&Call :Sub %%InnoSetupPath:*Z=%%)

IF DEFINED InnoSetupPath ("%InnoSetupPath%\iscc.exe" /Q /O"..\..\bin\Release"^
 "..\..\Build\Installer\Process_Hacker_installer.iss"&&(
	ECHO:Installer compiled successfully!)) ELSE (ECHO:%M_%)

:: Delete the temporary directory
rmdir tmp

:: ZIP the files
IF NOT DEFINED N_ (START "" /B /WAIT "..\..\Build\7za\7za.exe" a -tzip -mx=9^
 "processhacker-bin.zip"^
 "Assistant.exe" "base.txt" "CHANGELOG.txt" "Help.htm" "kprocesshacker.sys"^
 "LICENSE.txt" "NProcessHacker.dll" "ProcessHacker.exe" "README.txt" "structs.txt"^
 >NUL&&(
	ECHO:ZIP created successfully!))

:CLEANUP

:: Make a PDB zip
"..\..\Build\7za\7za.exe" a -tzip -mx=9 "processhacker-pdb.zip"^
 "*.pdb"^
 "..\..\..\KProcessHacker\i386\kprocesshacker.pdb"^
 "..\..\..\NProcessHacker\Release\NProcessHacker.pdb"^
 >NUL&&(ECHO:PDB ZIP created successfully!)

:END
GOTO :EOF

:Sub
SET InnoSetupPath=%*