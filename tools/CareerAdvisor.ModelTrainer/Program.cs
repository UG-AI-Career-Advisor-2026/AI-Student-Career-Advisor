using CareerAdvisor.Infrastructure.MachineLearning;

var repositoryRoot = FindRepositoryRoot(Directory.GetCurrentDirectory());

var datasetPath = ResolvePath(
    args.ElementAtOrDefault(0)
        ?? "data/training/sample-career-training-data.csv",
    repositoryRoot);

var modelPath = ResolvePath(
    args.ElementAtOrDefault(1)
        ?? "data/models/career-recommendation-model.zip",
    repositoryRoot);

var metadataPath = ResolvePath(
    args.ElementAtOrDefault(2)
        ?? "data/models/career-recommendation-model.metadata.json",
    repositoryRoot);

Directory.CreateDirectory(
    Path.GetDirectoryName(modelPath)
        ?? throw new InvalidOperationException(
            "The model output directory could not be determined."));

Directory.CreateDirectory(
    Path.GetDirectoryName(metadataPath)
        ?? throw new InvalidOperationException(
            "The metadata output directory could not be determined."));

Console.WriteLine("CareerIQ ML.NET model trainer");
Console.WriteLine();
Console.WriteLine(
    "This model uses synthetic data for demonstrating the academic MVP.");
Console.WriteLine($"Dataset: {datasetPath}");
Console.WriteLine($"Model: {modelPath}");
Console.WriteLine($"Metadata: {metadataPath}");
Console.WriteLine();

try
{
    var trainer = new CareerModelTrainer(
        CareerModelTrainer.DefaultSeed);

    var result = trainer.Train(
        datasetPath,
        modelPath,
        metadataPath);

    var metadata = result.Metadata;

    Console.WriteLine("Training completed successfully.");
    Console.WriteLine();
    Console.WriteLine($"Dataset version: {metadata.DatasetVersion}");
    Console.WriteLine($"Training date UTC: {metadata.TrainingDateUtc:O}");
    Console.WriteLine($"Trainer: {metadata.Trainer}");
    Console.WriteLine($"Random seed: {metadata.RandomSeed}");
    Console.WriteLine($"Total records: {metadata.TotalRecordCount}");
    Console.WriteLine($"Training records: {metadata.TrainingRecordCount}");
    Console.WriteLine($"Test records: {metadata.TestRecordCount}");
    Console.WriteLine($"Micro accuracy: {metadata.MicroAccuracy:F4}");
    Console.WriteLine($"Macro accuracy: {metadata.MacroAccuracy:F4}");
    Console.WriteLine($"Log-loss: {metadata.LogLoss:F4}");
    Console.WriteLine();
    Console.WriteLine("Score-vector label mapping:");

    for (var index = 0; index < metadata.ScoreLabels.Count; index++)
    {
        Console.WriteLine(
            $"  Score[{index}] = {metadata.ScoreLabels[index]}");
    }

    Console.WriteLine();
    Console.WriteLine($"Saved model: {result.ModelPath}");
    Console.WriteLine($"Saved metadata: {result.MetadataPath}");

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("Model training failed.");
    Console.Error.WriteLine(exception.Message);

    return 1;
}

static string FindRepositoryRoot(string startingDirectory)
{
    var directory = new DirectoryInfo(
        Path.GetFullPath(startingDirectory));

    while (directory is not null)
    {
        var solutionPath = Path.Combine(
            directory.FullName,
            "CareerAdvisor.sln");

        if (File.Exists(solutionPath))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException(
        "Could not locate the repository root containing CareerAdvisor.sln.");
}

static string ResolvePath(
    string path,
    string repositoryRoot)
{
    return Path.GetFullPath(
        Path.IsPathRooted(path)
            ? path
            : Path.Combine(repositoryRoot, path));
}
