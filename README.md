**Migration_Admin.exe**  
Run .exe, if prompted to install .NET 8 runtime, do so. Afterwards:\
\
Hit "Start Migration":
- Installs .NET 6 & 8 SDK
- Installs Google Chrome
- Prompts to remove local services
- Installs Skyview services located either on the Desktop or Desktop\migration\
- Starts Google Chrome
- Starts shell:startup
- Removes user "Kiosk"
- Renames current user to "Radianse"
- Removes password from user "Radianse"
