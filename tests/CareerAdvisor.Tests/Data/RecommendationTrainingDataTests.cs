using System.Globalization;
using CareerAdvisor.Core.Recommendations;
using CareerAdvisor.Infrastructure.Repositories;

namespace CareerAdvisor.Tests.Data;

public class RecommendationTrainingDataTests
{
    [Fact]
    public void Dataset_HasExactRequiredColumns()
    {
        var dataset = LoadDataset();

        Assert.Equal(
            RecommendationFeatureSchema.RequiredColumns.ToArray(),
            dataset.Header);
    }

    [Fact]
    public void Dataset_HasBalancedCareerRepresentation()
    {
        var dataset = LoadDataset();
        var labelIndex = GetColumnIndex(dataset.Header, "CareerLabel");

        Assert.True(
            dataset.Rows.Count >= 80,
            $"Dataset contains {dataset.Rows.Count} records; at least 80 are required.");

        var recognizedLabels = RecommendationFeatureSchema
            .CareerLabelsByCode.Values
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var groups = dataset.Rows
            .GroupBy(row => row[labelIndex], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase);

        Assert.Equal(recognizedLabels.Count, groups.Count);

        foreach (var label in recognizedLabels)
        {
            Assert.True(
                groups.TryGetValue(label, out var count),
                $"Career label '{label}' is missing from the dataset.");

            Assert.True(
                count >= RecommendationFeatureSchema.MinimumRecordsPerCareer,
                $"Career label '{label}' has only {count} records.");
        }

        Assert.Single(groups.Values.Distinct());
    }

    [Fact]
    public void Dataset_HasNoEmptyOrMalformedValues()
    {
        var dataset = LoadDataset();

        Assert.NotEmpty(dataset.Rows);

        for (var rowIndex = 0; rowIndex < dataset.Rows.Count; rowIndex++)
        {
            var row = dataset.Rows[rowIndex];

            Assert.True(
                row.Length == dataset.Header.Length,
                $"Row {rowIndex + 2} has {row.Length} values; " +
                $"{dataset.Header.Length} were expected.");

            for (var columnIndex = 0;
                 columnIndex < row.Length;
                 columnIndex++)
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(row[columnIndex]),
                    $"Row {rowIndex + 2}, column " +
                    $"'{dataset.Header[columnIndex]}' is empty.");
            }
        }
    }

    [Fact]
    public void Dataset_NumericValuesAreWithinDocumentedRange()
    {
        var dataset = LoadDataset();

        foreach (var column in RecommendationFeatureSchema.NumericColumns)
        {
            var columnIndex = GetColumnIndex(dataset.Header, column);

            for (var rowIndex = 0;
                 rowIndex < dataset.Rows.Count;
                 rowIndex++)
            {
                var valueText = dataset.Rows[rowIndex][columnIndex];

                Assert.True(
                    int.TryParse(
                        valueText,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var value),
                    $"Row {rowIndex + 2}, column '{column}' contains " +
                    $"non-numeric value '{valueText}'.");

                Assert.InRange(
                    value,
                    RecommendationFeatureSchema.MinimumNumericValue,
                    RecommendationFeatureSchema.MaximumNumericValue);
            }
        }
    }

    [Fact]
    public void Dataset_CategoricalValuesAreRecognized()
    {
        var dataset = LoadDataset();

        foreach (var mapping in RecommendationFeatureSchema
                     .AllowedCategoricalValuesByColumn)
        {
            var columnIndex = GetColumnIndex(
                dataset.Header,
                mapping.Key);

            for (var rowIndex = 0;
                 rowIndex < dataset.Rows.Count;
                 rowIndex++)
            {
                var value = dataset.Rows[rowIndex][columnIndex];

                Assert.Contains(
                    value,
                    mapping.Value,
                    StringComparer.Ordinal);
            }
        }

        var labelIndex = GetColumnIndex(dataset.Header, "CareerLabel");
        var recognizedLabels = RecommendationFeatureSchema
            .CareerLabelsByCode.Values
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.All(
            dataset.Rows,
            row => Assert.Contains(row[labelIndex], recognizedLabels));
    }

    [Fact]
    public async Task CareerCodeMappings_MatchCareerCatalogue()
    {
        var repository = new JsonCareerRepository(
            GetRepositoryFilePath("data", "career-catalog.json"));

        var catalogueMappings = (await repository.GetAllAsync())
            .ToDictionary(
                career => career.Code,
                career => career.Title,
                StringComparer.OrdinalIgnoreCase);

        Assert.Equal(
            RecommendationFeatureSchema.CareerLabelsByCode.Count,
            catalogueMappings.Count);

        foreach (var mapping in
                 RecommendationFeatureSchema.CareerLabelsByCode)
        {
            Assert.True(
                catalogueMappings.TryGetValue(
                    mapping.Key,
                    out var catalogueTitle),
                $"Career code '{mapping.Key}' is missing from the catalogue.");

            Assert.Equal(mapping.Value, catalogueTitle);
        }
    }

    private static TrainingDataset LoadDataset()
    {
        var path = GetRepositoryFilePath(
            "data",
            "training",
            "sample-career-training-data.csv");

        Assert.True(File.Exists(path), $"Dataset was not found at '{path}'.");

        var lines = File.ReadAllLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        Assert.True(lines.Length > 1, "Dataset has no training records.");

        var header = SplitCsvLine(lines[0]);
        var rows = lines
            .Skip(1)
            .Select(SplitCsvLine)
            .ToList();

        return new TrainingDataset(header, rows);
    }

    private static string[] SplitCsvLine(string line)
    {
        return line
            .Split(',', StringSplitOptions.None)
            .Select(value => value.Trim())
            .ToArray();
    }

    private static int GetColumnIndex(
        IReadOnlyList<string> header,
        string column)
    {
        var index = -1;

        for (var currentIndex = 0;
             currentIndex < header.Count;
             currentIndex++)
        {
            if (string.Equals(
                    header[currentIndex],
                    column,
                    StringComparison.Ordinal))
            {
                index = currentIndex;
                break;
            }
        }

        Assert.True(index >= 0, $"Required column '{column}' is missing.");
        return index;
    }

    private static string GetRepositoryFilePath(
        params string[] pathSegments)
    {
        var repositoryRoot = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                ".."));

        return Path.Combine(
            new[] { repositoryRoot }
                .Concat(pathSegments)
                .ToArray());
    }

    private sealed record TrainingDataset(
        string[] Header,
        List<string[]> Rows);
}