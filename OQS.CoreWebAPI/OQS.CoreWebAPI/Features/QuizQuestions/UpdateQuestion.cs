﻿using Carter;
using FluentValidation;
using Mapster;
using OQS.CoreWebAPI.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OQS.CoreWebAPI.Database;
using OQS.CoreWebAPI.Features.QuizQuestions;
using OQS.CoreWebAPI.Shared;
using OQS.CoreWebAPI.Entities;
using OQS.CoreWebAPI.Contracts.CRUD;

namespace OQS.CoreWebAPI.Features.QuizQuestions
{
    public static class UpdateQuestion
    {
        public class BodyUpdateQuestion : IRequest<Result<QuestionResponse>>
        {
            public string Text { get; set; } = string.Empty;
            public QuestionType Type { get; set; }
            public int AllocatedPoints { get; set; }
            public int TimeLimit { get; set; }
            public List<string>? Choices { get; set; } = new List<string>();
            public bool? TrueFalseAnswer { get; set; }
            public List<string>? MultipleChoiceAnswers { get; set; } = new List<string>();
            public string? SingleChoiceAnswer { get; set; } = string.Empty;
            public List<string>? WrittenAcceptedAnswers { get; set; } = new List<string>();
        }

        public class Command : IRequest<Result<QuestionResponse>>
        {
            public Guid Id { get; set; }
            public Guid QuizId { get; set; }
            public BodyUpdateQuestion Body { get; set; } = new BodyUpdateQuestion();
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Id)
                    .NotEmpty().WithMessage("Id is required.");

                RuleFor(x => x.QuizId)
                    .NotEmpty().WithMessage("QuizId is required.");

                RuleFor(x => x.Body)
                    .NotNull().WithMessage("Body is required.")
                    .DependentRules(() =>
                    {
                        RuleFor(x => x.Body.Text)
                            .NotEmpty().WithMessage("Text is required.");

                        RuleFor(x => x.Body.Type)
                            .NotEmpty().WithMessage("Type is required.");

                        RuleFor(x => x.Body.AllocatedPoints)
                            .GreaterThan(0).WithMessage("AllocatedPoints must be greater than 0.");

                        RuleFor(x => x.Body.TimeLimit)
                            .GreaterThan(0).WithMessage("TimeLimit must be greater than 0.");

                        When(x => x.Body.Choices != null, () =>
                        {
                            RuleFor(x => x.Body.Choices)
                                .NotEmpty().WithMessage("Choices are required.");
                        });

                        When(x => x.Body.MultipleChoiceAnswers != null, () =>
                        {
                            RuleFor(x => x.Body.MultipleChoiceAnswers)
                                .NotEmpty().WithMessage("MultipleChoiceAnswers are required.");
                        });

                        When(x => x.Body.SingleChoiceAnswer != null, () =>
                        {
                            RuleFor(x => x.Body.SingleChoiceAnswer)
                                .NotEmpty().WithMessage("SingleChoiceAnswer is required.");
                        });

                        When(x => x.Body.TrueFalseAnswer != null, () =>
                        {
                            RuleFor(x => x.Body.TrueFalseAnswer)
                                .NotEmpty().WithMessage("TrueFalseAnswer is required.");
                        });

                        When(x => x.Body.WrittenAcceptedAnswers != null, () =>
                        {
                            RuleFor(x => x.Body.WrittenAcceptedAnswers)
                                .NotEmpty().WithMessage("WrittenAcceptedAnswers are required.");
                        });
                    });
            }
        }

        internal sealed class Handler : IRequestHandler<Command, Result<QuestionResponse>>
        {
            private readonly ApplicationDbContext context;
            private readonly IValidator<Command> validator;

            public Handler(ApplicationDbContext context, IValidator<Command> validator)
            {
                this.context = context;
                this.validator = validator;
            }

            public async Task<Result<QuestionResponse>> Handle(Command request, CancellationToken cancellationToken)
            {
                var validationResult = validator.Validate(request);
                /* if (!validationResult.IsValid)
                 {
                     return Result.Failure<QuestionResponse>(
                         new Error(
                             400, "validation failed"
                         ));
                 }*/

                var question = await context.Questions
                    .AsNoTracking()
                    .Where(question => question.Id == request.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                if (question is null)
                {
                    return Result.Failure<QuestionResponse>(
                        new Error(
                            "404", "Question not found"
                        ));
                }

                try
                {
                    if (request.Body.Text != string.Empty)
                        question.Text = request.Body.Text;

                    if (request.Body.AllocatedPoints != 0)
                        question.AllocatedPoints = request.Body.AllocatedPoints;

                    if (request.Body.TimeLimit != 0)
                        question.TimeLimit = request.Body.TimeLimit;

                    if (request.Body.Type != default)
                    {
                        question.Type = request.Body.Type;
                        // Update question based on its type
                        switch (request.Body.Type)
                        {
                            case QuestionType.TrueFalse:
                                question = new TrueFalseQuestion(Guid.NewGuid(), request.Body.Text, request.QuizId,
                                    request.Body.TimeLimit, request.Body.AllocatedPoints,
                                    request.Body.TrueFalseAnswer ?? false);
                                // _dbContext.TrueFalseQuestions.Add((TrueFalseQuestion)question);
                                break;
                            case QuestionType.MultipleChoice:
                                question = new MultipleChoiceQuestion(Guid.NewGuid(), request.Body.Text, request.QuizId,
                                    request.Body.TimeLimit, request.Body.AllocatedPoints,
                                    request.Body.Choices ?? new List<string>(),
                                    request.Body.MultipleChoiceAnswers ?? new List<string>());
                                //  _dbContext.MultipleChoiceQuestions.Add((MultipleChoiceQuestion)question);
                                break;
                            case QuestionType.SingleChoice:
                                question = new SingleChoiceQuestion(Guid.NewGuid(), request.Body.Text, request.QuizId,
                                    request.Body.TimeLimit, request.Body.AllocatedPoints,
                                    request.Body.Choices ?? new List<string>(),
                                    request.Body.SingleChoiceAnswer ?? string.Empty);
                                // _dbContext.SingleChoiceQuestions.Add((SingleChoiceQuestion)question);
                                break;
                            case QuestionType.WrittenAnswer:
                                question = new WrittenAnswerQuestion(Guid.NewGuid(), request.Body.Text, request.QuizId,
                                    request.Body.TimeLimit, request.Body.AllocatedPoints,
                                    request.Body.WrittenAcceptedAnswers ?? new List<string>());
                                // _dbContext.WrittenAnswerQuestions.Add((WrittenAnswerQuestion)question);
                                break;
                            case QuestionType.ReviewNeeded:
                                question = new ReviewNeededQuestion(Guid.NewGuid(), request.Body.Text, request.QuizId,
                                    request.Body.TimeLimit, request.Body.AllocatedPoints);
                                // _dbContext.ReviewNeededQuestions.Add((ReviewNeededQuestion)question);
                                break;
                            default:
                                return Result.Failure<QuestionResponse>(
                                    new Error("400", "Invalid question type"));
                        }

                        /*     if (request.Body.Choices != null && request.Body.Choices.Any())
                                 question.Choices = request.Body.Choices;

                             if (request.Body.TrueFalseAnswer != null)
                                 question.TrueFalseAnswer = request.Body.TrueFalseAnswer;

                             if (request.Body.MultipleChoiceAnswers != null && request.Body.MultipleChoiceAnswers.Any())
                                 question.MultipleChoiceAnswers = request.Body.MultipleChoiceAnswers;

                             if (request.Body.SingleChoiceAnswer != null)
                                 question.SingleChoiceAnswer = request.Body.SingleChoiceAnswer;

                             if (request.Body.WrittenAcceptedAnswers != null && request.Body.WrittenAcceptedAnswers.Any())
                                 question.WrittenAcceptedAnswers = request.Body.WrittenAcceptedAnswers;
                        */

                        context.Questions.Update(question);
                        await context.SaveChangesAsync(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    return Result.Failure<QuestionResponse>(
                        new Error(
                            "400", ex.Message
                        ));
                }

                var questionResponse = new QuestionResponse(question);
                return Result.Success(questionResponse);
            }
        }
    }
}

public class UpdateQuestionEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("api/quizzes/{quiz_id}/questions/{question_id}",
            async (Guid quiz_id, Guid question_id, UpdateQuestionRequest request, ISender sender) =>
            {
                var bodyUpdateQuiz = request.Adapt<UpdateQuestion.BodyUpdateQuestion>();

                var command = new UpdateQuestion.Command
                {
                    QuizId = quiz_id,
                    Id = question_id,
                    Body = bodyUpdateQuiz
                };

                var result = await sender.Send(command);

                if (result.IsFailure)
                {
                    return Results.NotFound(result.Error);
                }

                return Results.Ok(result.Value);
            });
    }
}