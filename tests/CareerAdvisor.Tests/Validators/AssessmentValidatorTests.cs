using Xunit;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Validators;
using System;
using System.Collections.Generic;

namespace CareerAdvisor.Tests.Validators;

public class AssessmentValidatorTests
{
    private readonly AssessmentValidator _validator;
    private readonly List<AssessmentQuestion> _allQuestions;

    public AssessmentValidatorTests()
    {
        _validator = new AssessmentValidator();
        _allQuestions = AssessmentQuestionBank.GetAllQuestions();
    }

    // ========== ValidateResponse Tests ==========

    [Fact]
    public void ValidateResponse_ValidResponseToFirstQuestion_ReturnsValid()
    {
        // Arrange
        var session = new AssessmentSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = Guid.NewGuid(),
            Responses = new List<AssessmentResponse>()
        };

        var question = _allQuestions[0];
        var option = question.Options[0];

        // Act
        var result = _validator.ValidateResponse(session, question.Id, option.Id, _allQuestions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateResponse_InvalidQuestionId_ReturnsInvalid()
    {
        // Arrange
        var session = new AssessmentSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = Guid.NewGuid(),
            Responses = new List<AssessmentResponse>()
        };

        var invalidQuestionId = Guid.NewGuid();
        var validOptionId = _allQuestions[0].Options[0].Id;

        // Act
        var result = _validator.ValidateResponse(session, invalidQuestionId, validOptionId, _allQuestions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("does not exist", result.Errors[0]);
    }

    [Fact]
    public void ValidateResponse_InvalidOptionId_ReturnsInvalid()
    {
        // Arrange
        var session = new AssessmentSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = Guid.NewGuid(),
            Responses = new List<AssessmentResponse>()
        };

        var question = _allQuestions[0];
        var invalidOptionId = Guid.NewGuid();

        // Act
        var result = _validator.ValidateResponse(session, question.Id, invalidOptionId, _allQuestions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("does not exist for question", result.Errors[0]);
    }

    [Fact]
    public void ValidateResponse_DuplicateResponse_ReturnsInvalid()
    {
        // Arrange
        var question = _allQuestions[0];
        var option = question.Options[0];

        var session = new AssessmentSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = Guid.NewGuid(),
            Responses = new List<AssessmentResponse>
            {
                new AssessmentResponse
                {
                    Id = Guid.NewGuid(),
                    AssessmentSessionId = Guid.Empty,
                    QuestionId = question.Id,
                    OptionId = option.Id
                }
            }
        };

        // Act
        var result = _validator.ValidateResponse(session, question.Id, option.Id, _allQuestions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Duplicate response detected", result.Errors[0]);
    }

    [Fact]
    public void ValidateResponse_EmptyOptionValue_ReturnsInvalid()
    {
        // Arrange
        var session = new AssessmentSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = Guid.NewGuid(),
            Responses = new List<AssessmentResponse>()
        };

        var questionWithEmptyOption = new AssessmentQuestion
        {
            Id = Guid.NewGuid(),
            Code = "TEST_Q",
            Text = "Test Question",
            Category = "Interests",
            IsRequired = true,
            DisplayOrder = 1,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption
                {
                    Id = Guid.NewGuid(),
                    Code = "TEST_OPT_A",
                    Value = "",  // Empty value
                    QuestionId = Guid.Empty
                }
            }
        };

        var questionsWithEmpty = new List<AssessmentQuestion> { questionWithEmptyOption };

        // Act
        var result = _validator.ValidateResponse(
            session,
            questionWithEmptyOption.Id,
            questionWithEmptyOption.Options[0].Id,
            questionsWithEmpty);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("cannot be empty", result.Errors[0]);
    }

    [Fact]
    public void ValidateResponse_MultipleValidResponses_AllValid()
    {
        // Arrange
        var session = new AssessmentSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = Guid.NewGuid(),
            Responses = new List<AssessmentResponse>()
        };

        var question1 = _allQuestions[0];
        var option1 = question1.Options[0];

        var question2 = _allQuestions[1];
        var option2 = question2.Options[1];

        // Act
        var result1 = _validator.ValidateResponse(session, question1.Id, option1.Id, _allQuestions);
        session.Responses.Add(new AssessmentResponse
        {
            AssessmentSessionId = session.Id,
            QuestionId = question1.Id,
            OptionId = option1.Id
        });

        var result2 = _validator.ValidateResponse(session, question2.Id, option2.Id, _allQuestions);

        // Assert
        Assert.True(result1.IsValid);
        Assert.True(result2.IsValid);
    }

    // ========== ValidateSessionCompletion Tests ==========

    [Fact]
    public void ValidateSessionCompletion_AllRequiredQuestionsAnswered_ReturnsValid()
    {
        // Arrange
        var session = new AssessmentSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = Guid.NewGuid(),
            Status = "InProgress",
            Responses = new List<AssessmentResponse>()
        };

        // Add responses for all required questions
        foreach (var question in _allQuestions.Where(q => q.IsRequired))
        {
            var option = question.Options[0];
            session.Responses.Add(new AssessmentResponse
            {
                AssessmentSessionId = session.Id,
                QuestionId = question.Id,
                OptionId = option.Id
            });
        }

        // Act
        var result = _validator.ValidateSessionCompletion(session, _allQuestions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateSessionCompletion_SessionNotInProgress_ReturnsInvalid()
    {
        // Arrange
        var session = new AssessmentSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = Guid.NewGuid(),
            Status = "Completed",
            Responses = new List<AssessmentResponse>()
        };

        // Act
        var result = _validator.ValidateSessionCompletion(session, _allQuestions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("not in progress", result.Errors[0]);
    }

    [Fact]
    public void ValidateSessionCompletion_NoResponses_ReturnsInvalid()
    {
        // Arrange
        var session = new AssessmentSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = Guid.NewGuid(),
            Status = "InProgress",
            Responses = new List<AssessmentResponse>()
        };

        // Act
        var result = _validator.ValidateSessionCompletion(session, _allQuestions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("no responses", result.Errors[0]);
    }

    [Fact]
    public void ValidateSessionCompletion_MissingRequiredQuestion_ReturnsInvalid()
    {
        // Arrange
        var session = new AssessmentSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = Guid.NewGuid(),
            Status = "InProgress",
            Responses = new List<AssessmentResponse>()
        };

        // Add responses for all but one required question
        var requiredQuestions = _allQuestions.Where(q => q.IsRequired).ToList();
        var questionsToAnswer = requiredQuestions.Skip(1).ToList();

        foreach (var question in questionsToAnswer)
        {
            var option = question.Options[0];
            session.Responses.Add(new AssessmentResponse
            {
                AssessmentSessionId = session.Id,
                QuestionId = question.Id,
                OptionId = option.Id
            });
        }

        // Act
        var result = _validator.ValidateSessionCompletion(session, _allQuestions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("not answered", result.Errors[0]);
        Assert.Contains(requiredQuestions[0].Code, result.Errors[0]);
    }

    [Fact]
    public void ValidateSessionCompletion_PartialResponses_ReturnsInvalid()
    {
        // Arrange
        var session = new AssessmentSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = Guid.NewGuid(),
            Status = "InProgress",
            Responses = new List<AssessmentResponse>()
        };

        // Add response for only first 3 questions
        var questionsToAnswer = _allQuestions.Take(3).ToList();
        foreach (var question in questionsToAnswer)
        {
            var option = question.Options[0];
            session.Responses.Add(new AssessmentResponse
            {
                AssessmentSessionId = session.Id,
                QuestionId = question.Id,
                OptionId = option.Id
            });
        }

        // Act
        var result = _validator.ValidateSessionCompletion(session, _allQuestions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("not answered", result.Errors[0]);
    }

    // ========== ValidateResponseIntegrity Tests ==========

    [Fact]
    public void ValidateResponseIntegrity_ValidResponse_ReturnsValid()
    {
        // Arrange
        var response = new AssessmentResponse
        {
            AssessmentSessionId = Guid.NewGuid(),
            QuestionId = Guid.NewGuid(),
            OptionId = Guid.NewGuid()
        };

        // Act
        var result = _validator.ValidateResponseIntegrity(response);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateResponseIntegrity_EmptyAssessmentSessionId_ReturnsInvalid()
    {
        // Arrange
        var response = new AssessmentResponse
        {
            AssessmentSessionId = Guid.Empty,
            QuestionId = Guid.NewGuid(),
            OptionId = Guid.NewGuid()
        };

        // Act
        var result = _validator.ValidateResponseIntegrity(response);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("AssessmentSessionId cannot be empty.", result.Errors);
    }

    [Fact]
    public void ValidateResponseIntegrity_EmptyQuestionId_ReturnsInvalid()
    {
        // Arrange
        var response = new AssessmentResponse
        {
            AssessmentSessionId = Guid.NewGuid(),
            QuestionId = Guid.Empty,
            OptionId = Guid.NewGuid()
        };

        // Act
        var result = _validator.ValidateResponseIntegrity(response);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("QuestionId cannot be empty.", result.Errors);
    }

    [Fact]
    public void ValidateResponseIntegrity_EmptyOptionId_ReturnsInvalid()
    {
        // Arrange
        var response = new AssessmentResponse
        {
            AssessmentSessionId = Guid.NewGuid(),
            QuestionId = Guid.NewGuid(),
            OptionId = Guid.Empty
        };

        // Act
        var result = _validator.ValidateResponseIntegrity(response);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("OptionId cannot be empty.", result.Errors);
    }

    [Fact]
    public void ValidateResponseIntegrity_MultipleEmptyIds_ReturnsAllErrors()
    {
        // Arrange
        var response = new AssessmentResponse
        {
            AssessmentSessionId = Guid.Empty,
            QuestionId = Guid.Empty,
            OptionId = Guid.Empty
        };

        // Act
        var result = _validator.ValidateResponseIntegrity(response);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
        Assert.Contains("AssessmentSessionId cannot be empty.", result.Errors);
        Assert.Contains("QuestionId cannot be empty.", result.Errors);
        Assert.Contains("OptionId cannot be empty.", result.Errors);
    }

    // ========== Additional Edge Case Tests ==========

    [Fact]
    public void ValidateResponse_OptionFromDifferentQuestion_ReturnsInvalid()
    {
        // Arrange
        var session = new AssessmentSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = Guid.NewGuid(),
            Responses = new List<AssessmentResponse>()
        };

        var question1 = _allQuestions[0];
        var question2 = _allQuestions[1];
        var optionFromQuestion2 = question2.Options[0];

        // Act - try to submit option from question2 as answer to question1
        var result = _validator.ValidateResponse(session, question1.Id, optionFromQuestion2.Id, _allQuestions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("does not exist for question", result.Errors[0]);
    }

    [Fact]
    public void ValidateSessionCompletion_AllQuestionsAnswered_ReturnsValid()
    {
        // Arrange
        var session = new AssessmentSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = Guid.NewGuid(),
            Status = "InProgress",
            Responses = new List<AssessmentResponse>()
        };

        // Answer all questions
        foreach (var question in _allQuestions)
        {
            var option = question.Options[0];
            session.Responses.Add(new AssessmentResponse
            {
                AssessmentSessionId = session.Id,
                QuestionId = question.Id,
                OptionId = option.Id
            });
        }

        // Act
        var result = _validator.ValidateSessionCompletion(session, _allQuestions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
