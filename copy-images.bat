@echo off
echo Setting up product images directory...
mkdir wwwroot\images\products 2>nul

echo Copying product images from women_images...
xcopy /y "C:\DatasetLicenta\women_images\*.jpg" wwwroot\images\products\
xcopy /y "C:\DatasetLicenta\women_images\*.png" wwwroot\images\products\

echo Done!
pause