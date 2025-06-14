# Migration_Bootstrapper.bat

Run first, checks/installs .NET 8 runtime for migration software to work (FDD), then runs migration tool.

# Migration_Admin.exe

Hit "Start Migration":

- Installs .NET 6 & 8 SDK
- Installs Google Chrome
- Removes user "Kiosk"
- Renames current user to "Radianse"
- Removes password from user "Radianse"
- Removes local programs and services related to Radianse
- Stops and Removes local services with keywords [radianse, airpointe, tanning, massage]
- Installs Skyview services located either on the Desktop or Desktop\migration\
- Starts Google Chrome
- Starts shell:startup
