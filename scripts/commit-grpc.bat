@echo off
echo Committing gRPC updates...
git commit -m "docs: make gRPC required protocol with multi-language examples"
if %errorlevel% equ 0 (
    echo Commit successful!
    echo Pushing to GitHub...
    git push origin master
    if %errorlevel% equ 0 (
        echo Push successful!
    ) else (
        echo Push failed!
    )
) else (
    echo Commit failed or nothing to commit!
)
pause
