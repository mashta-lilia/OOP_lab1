#!/usr/bin/env sh
# Перегенерує C#-парсер з граматики. Потрібна Java 11+.
# Запуск з кореня репозиторію: sh tools/generate-parser.sh
set -e
JAR="tools/antlr-4.13.1-complete.jar"
if [ ! -f "$JAR" ]; then
  curl -fsSL -o "$JAR" https://repo1.maven.org/maven2/org/antlr/antlr4/4.13.1/antlr4-4.13.1-complete.jar
fi
rm -rf LabCalculator.Core/Parsing/Generated
cd LabCalculator.Core/Grammar
java -jar ../../$JAR -Dlanguage=CSharp -visitor -no-listener \
  -package LabCalculator.Core.Parsing -o ../Parsing/Generated LabCalculator.g4
cd ../Parsing/Generated
rm -f ./*.interp ./*.tokens
