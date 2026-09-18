using RH_CM.Service.Trainify;

namespace RH_CM.Tests;

/// <summary>
/// Cubre las reglas de negocio puras extraídas de TrainifyController al mover el motor de
/// Diagnóstico/Examen a DiagnosticExamService: calificación de respuestas, cálculo de score
/// y el umbral mínimo para aprobar. No toca la base de datos.
/// </summary>
public class DiagnosticExamServiceTests
{
    [Fact]
    public void IsAnswerCorrect_ExactMatch_ReturnsTrue()
    {
        Assert.True(DiagnosticExamService.IsAnswerCorrect(new[] { 1, 2 }, new[] { 2, 1 }));
    }

    [Fact]
    public void IsAnswerCorrect_MissingASelectedCorrectOption_ReturnsFalse()
    {
        Assert.False(DiagnosticExamService.IsAnswerCorrect(new[] { 1 }, new[] { 1, 2 }));
    }

    [Fact]
    public void IsAnswerCorrect_ExtraIncorrectOptionSelected_ReturnsFalse()
    {
        Assert.False(DiagnosticExamService.IsAnswerCorrect(new[] { 1, 2, 3 }, new[] { 1, 2 }));
    }

    [Fact]
    public void IsAnswerCorrect_NoSelection_MatchesOnlyWhenNothingIsCorrect()
    {
        Assert.True(DiagnosticExamService.IsAnswerCorrect(Array.Empty<int>(), Array.Empty<int>()));
        Assert.False(DiagnosticExamService.IsAnswerCorrect(Array.Empty<int>(), new[] { 1 }));
    }

    [Theory]
    [InlineData(10, 10, 100)]
    [InlineData(0, 10, 0)]
    [InlineData(8, 10, 80)]
    [InlineData(1, 3, 33)] // Math.Round: 33.33 -> 33
    [InlineData(2, 3, 67)] // 66.67 -> 67
    public void CalculateScore_MatchesExpectedRounding(int correct, int total, int expected)
    {
        Assert.Equal(expected, DiagnosticExamService.CalculateScore(correct, total));
    }

    [Fact]
    public void CalculateScore_ZeroQuestions_DoesNotDivideByZero()
    {
        // El original usa Math.Max(1, totalQuestions) precisamente para este caso.
        Assert.Equal(0, DiagnosticExamService.CalculateScore(0, 0));
    }

    [Theory]
    [InlineData(80, true)]
    [InlineData(81, true)]
    [InlineData(100, true)]
    [InlineData(79, false)]
    [InlineData(0, false)]
    public void IsPassingScore_UsesMinimumEightyThreshold(int score, bool expectedPassing)
    {
        Assert.Equal(expectedPassing, DiagnosticExamService.IsPassingScore(score));
    }

    [Theory]
    [InlineData(79, 5)] // PENDING: the attempt exists, but the course is not complete
    [InlineData(80, 1)] // COMPLETED
    [InlineData(100, 1)]
    public void CourseStatusForScore_DoesNotMarkFailedAttemptsCompleted(int score, int expectedStatus)
    {
        Assert.Equal(expectedStatus, DiagnosticExamService.CourseStatusForScore(score));
    }

    [Fact]
    public void HasExactDistinctIds_AcceptsSameSetInDifferentOrder()
    {
        Assert.True(DiagnosticExamService.HasExactDistinctIds(new[] { 3, 1, 2 }, new[] { 1, 2, 3 }));
    }

    [Theory]
    [InlineData(new[] { 1, 2 }, new[] { 1, 2, 3 })]
    [InlineData(new[] { 1, 2, 99 }, new[] { 1, 2, 3 })]
    [InlineData(new[] { 1, 1, 2 }, new[] { 1, 2, 3 })]
    public void HasExactDistinctIds_RejectsMissingForeignOrDuplicateQuestions(int[] submitted, int[] expected)
    {
        Assert.False(DiagnosticExamService.HasExactDistinctIds(submitted, expected));
    }
}
