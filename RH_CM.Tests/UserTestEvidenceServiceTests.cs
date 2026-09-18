using RH_CM.Models;
using RH_CM.Service.UserTestEvidence;

namespace RH_CM.Tests;

/// <summary>
/// Cubre GroupOptionsByQuestion, la lógica que reemplazó la consulta N+1 dentro del foreach
/// de GetDiagnosticExamAsync/GetExamAsync (una consulta por pregunta -> una sola consulta batched).
/// No toca la base de datos: opera sobre listas en memoria.
/// </summary>
public class UserTestEvidenceServiceTests
{
    private static CtQuestion Question(int id) => new CtQuestion { PkQuestions = id };

    private static CtOption Option(int id, int questionId) => new CtOption
    {
        PkOptions = id,
        FkQuestions = questionId,
        Options = $"Option {id}",
        Answer = 0
    };

    [Fact]
    public void GroupsMultipleOptionsUnderTheSameQuestion()
    {
        var q1 = Question(1);
        var rows = new List<(CtQuestion Question, CtOption Option)>
        {
            (q1, Option(10, 1)),
            (q1, Option(11, 1)),
            (q1, Option(12, 1)),
        };

        var result = UserTestEvidenceService.GroupOptionsByQuestion(rows);

        Assert.Single(result);
        Assert.True(result.ContainsKey(1));
        Assert.Equal(new[] { 10, 11, 12 }, result[1].Select(o => o.PkOptions));
    }

    [Fact]
    public void KeepsQuestionsSeparate()
    {
        var q1 = Question(1);
        var q2 = Question(2);
        var rows = new List<(CtQuestion Question, CtOption Option)>
        {
            (q1, Option(10, 1)),
            (q2, Option(20, 2)),
            (q2, Option(21, 2)),
        };

        var result = UserTestEvidenceService.GroupOptionsByQuestion(rows);

        Assert.Equal(2, result.Count);
        Assert.Single(result[1]);
        Assert.Equal(2, result[2].Count);
    }

    [Fact]
    public void DropsRowsWithNoOption_FromLeftJoinWithNoMatch()
    {
        // Simula el LEFT JOIN: una pregunta sin ninguna opción cargada produce Option = null.
        var q1 = Question(1);
        var q2 = Question(2);
        var rows = new List<(CtQuestion Question, CtOption Option)>
        {
            (q1, Option(10, 1)),
            (q2, null!),
        };

        var result = UserTestEvidenceService.GroupOptionsByQuestion(rows);

        Assert.Single(result);
        Assert.True(result.ContainsKey(1));
        Assert.False(result.ContainsKey(2));
    }

    [Fact]
    public void EmptyInput_ReturnsEmptyDictionary()
    {
        var result = UserTestEvidenceService.GroupOptionsByQuestion(
            Enumerable.Empty<(CtQuestion Question, CtOption Option)>());

        Assert.Empty(result);
    }

    [Fact]
    public void QuestionWithNoEntry_LookupMissesGracefully()
    {
        // Refleja el uso en el controlador: TryGetValue sobre una pregunta que no está en el diccionario
        // debe comportarse como "sin opciones", no lanzar.
        var rows = new List<(CtQuestion Question, CtOption Option)>
        {
            (Question(1), Option(10, 1)),
        };

        var result = UserTestEvidenceService.GroupOptionsByQuestion(rows);

        var found = result.TryGetValue(999, out var options);

        Assert.False(found);
        Assert.Null(options);
    }
}
