# Перегенерує C#-парсер з граматики. Потрібна Java 11+.
# Запуск з кореня репозиторію: powershell -ExecutionPolicy Bypass -File tools/generate-parser.ps1
$ErrorActionPreference = "Stop"
$jar = "tools/antlr-4.13.1-complete.jar"
if (-not (Test-Path $jar)) {
    Invoke-WebRequest -Uri "https://repo1.maven.org/maven2/org/antlr/antlr4/4.13.1/antlr4-4.13.1-complete.jar" -OutFile $jar
}
$jarPath = (Resolve-Path $jar).Path
Remove-Item -Recurse -Force "LabCalculator.Core/Parsing/Generated" -ErrorAction SilentlyContinue
Push-Location "LabCalculator.Core/Grammar"
try {
    java -jar $jarPath -Dlanguage=CSharp -visitor -no-listener `
        -package LabCalculator.Core.Parsing -o ../Parsing/Generated LabCalculator.g4
}
finally { Pop-Location }
Remove-Item "LabCalculator.Core/Parsing/Generated/*.interp", "LabCalculator.Core/Parsing/Generated/*.tokens" -ErrorAction SilentlyContinue
