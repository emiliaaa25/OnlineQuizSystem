﻿namespace OQS.CoreWebAPI.Entities.ResultsAndStatistics.QuestionResults
{
    public class WrittenAnswerQuestionResult : QuestionResultBase
    {
        public string WrittenAnswer { get; set; }
        public AnswerResult WrittenAnswerResult { get; set; }

        public WrittenAnswerQuestionResult(Guid resultId, Guid userId, Guid questionId, float score, string writtenAnswer, AnswerResult writtenAnswerResult) :
            base(resultId, userId, questionId, score)
        {
            WrittenAnswer = writtenAnswer;
            WrittenAnswerResult = writtenAnswerResult;
        }

        public WrittenAnswerQuestionResult(Guid userId, Guid questionId, float score, string writtenAnswer, AnswerResult writtenAnswerResult) :
            base(userId, questionId, score)
        {
            WrittenAnswer = writtenAnswer;
            WrittenAnswerResult = writtenAnswerResult;
        }
    }

}
