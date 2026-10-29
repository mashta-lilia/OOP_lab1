using LabCalculator.Experiments;

// Запуск з кореня репозиторію: dotnet run -c Release --project experiments/LabCalculator.Experiments
Console.OutputEncoding = System.Text.Encoding.UTF8;

PrecisionExperiment.Run();
Console.WriteLine();
RecalculationExperiment.Run();
