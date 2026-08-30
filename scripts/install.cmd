@echo off
:: ============================================
:: YAPYAP Head Tracking - Install
:: ============================================
:: Thin wrapper - install body lives in cameraunlock-core/scripts/install-body-bepinex.cmd,
:: staged into the release ZIP's shared/ by Copy-SharedBundle. To change
:: install behaviour edit the body, not this wrapper. Everything below the
:: CONFIG BLOCK is copied verbatim from
:: cameraunlock-core/scripts/templates/install-wrapper-bepinex.cmd.
:: ============================================

:: --- CONFIG BLOCK ---
set "GAME_ID=yapyap"
set "MOD_DISPLAY_NAME=YAPYAP Head Tracking"
set "MOD_DLLS=YapyapHeadTracking.dll CameraUnlock.Core.dll CameraUnlock.Core.Unity.dll"
set "MOD_INTERNAL_NAME=YapyapHeadTracking"
set "MOD_VERSION=0.0.0"
set "STATE_FILE=.headtracking-state.json"
set "FRAMEWORK_TYPE=BepInEx"
set "BEPINEX_ARCH=x64"
set "BEPINEX_VENDOR_ZIP_NAME="
set "BEPINEX_SUBFOLDER="
set "PLUGIN_SUBFOLDER="
set "MOD_CONTROLS=Controls:&echo   End  / Ctrl+Shift+Y - Toggle tracking&echo   PgUp / Ctrl+Shift+G - Cycle tracking mode&echo   PgDn / Ctrl+Shift+H - Toggle yaw mode"
:: --- END CONFIG BLOCK ---

set "WRAPPER_DIR=%~dp0"
set "_BODY=%WRAPPER_DIR%shared\install-body-bepinex.cmd"
if not exist "%_BODY%" set "_BODY=%WRAPPER_DIR%..\cameraunlock-core\scripts\install-body-bepinex.cmd"
if not exist "%_BODY%" (
    echo ERROR: install-body-bepinex.cmd not found in shared\ or ..\cameraunlock-core\scripts\.
    echo If this is a release ZIP, re-download it from GitHub ^(corrupt installer^).
    echo If this is the dev tree, run: git submodule update --init --recursive
    exit /b 1
)
call "%_BODY%" %*
exit /b %errorlevel%
