﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OrcaQuiz.Models;
using OrcaQuiz.Models.Enums;
using OrcaQuiz.Repositories;
using OrcaQuiz.Utils;
using OrcaQuiz.ViewModels;

namespace OrcaQuiz.Controllers
{
    public class AdminController : Controller
    {
        IOrcaQuizRepository repository;
        IHostingEnvironment env;

        public AdminController(IOrcaQuizRepository repository, IHostingEnvironment env)
        {
            this.env = env;
            this.repository = repository;
        }

        [Route("Admin/ShowResults/{testId}")]
        public IActionResult ShowResults(int testId)
        {
            var vm = repository.GetShowResultsVM(testId);
            return View(vm);
        }

        [Route("Admin/Test/{testId}/Question/Create")]
        public IActionResult CreateQuestion(int testId)
        {
            int questionId = repository.CreateTestQuestion(testId);

            return RedirectToAction(nameof(EditQuestion), new { testId = testId, questionId = questionId });
        }

        [HttpPost]
        public IActionResult EditQuestionSettings(int testId, int questionId, EditQuestionVM viewModel)
        {
            repository.EditQuestion(testId, questionId, viewModel);

            return RedirectToAction(nameof(EditQuestion), new { testId = testId, questionId = questionId });
        }

        //[HttpPost]
        //public PartialViewResult EditQuestionText(int questionId, string questionText)
        //{
        //    var thisQuestion = repository.GetAllQuestions().SingleOrDefault(o => o.Id == questionId);
        //    thisQuestion.QuestionText = questionText;

        //    var model = new QuestionFormVM()
        //    {
        //        QuestionText = questionText,
        //        IsInEditQuestion = true,
        //        QuestionType = thisQuestion.QuestionType
        //    };

        //    return PartialView("_QuestionFormPartial", model);
        //}

        public PartialViewResult EditAnswer(int questionId, int answerId, string answerText, int sortOrder, bool isCorrect)
        {
            var model = repository.EditAnswer(questionId, answerId, answerText, sortOrder, isCorrect);
            return PartialView("_AnswerFormPartial", model);
        }

        public IActionResult CreateEmptyAnswer(int testId, int questionId)
        {
            int answerId = repository.CreateAnswer(questionId);
            return RedirectToAction(nameof(EditQuestion), new { testId = testId, questionId = questionId });
        }

        public IActionResult RemoveAnswer(int testId, int questionId, int answerId)
        {
            repository.RemoveAnswerFromQuestion(testId, questionId, answerId);
            return RedirectToAction(nameof(EditQuestion), new { testId = testId, questionId = questionId });
        }

        [Route("Admin/Test/{testId}/Question/{questionId}/Edit")]
        public IActionResult EditQuestion(int testId, int questionId)
        {
            var viewModel = repository.GetEditQuestionVM(testId, questionId);

            return View(viewModel);
        }

        [Route("Admin/Test/{testId}/Settings")]
        public IActionResult EditTestSettings(int testId)
        {
            var model = repository.GetTestSettingsFormVM(testId);                
            return View(model);
        }

        [Route("Admin/Test/{testId}/Settings")]
        [HttpPost]
        public IActionResult EditTestSettings(TestSettingsFormVM viewModel, int testId)
        {
            repository.EditTestSettings(viewModel, testId);
            return RedirectToAction(nameof(AdminController.ManageTestQuestions), new { testId = testId });
        }

        [Route("Admin/Test/Create")]
        public IActionResult CreateTest()
        {
            return View();
        }

        [Route("Admin/Test/Create")]
        [HttpPost]
        public IActionResult CreateTest(TestSettingsFormVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var testId = repository.CreateTest(model, User.Identity.Name);
            
            return RedirectToAction(nameof(AdminController.ManageTestQuestions), new { testId = testId });
        }

        [Route("Admin/Test/{testId}")]
        public IActionResult ManageTestQuestions(int testId)
        {
            var viewModel = repository.GetManageTestQuestionVM(testId);

            return View(viewModel);
        }

        public IActionResult RemoveQuestion(int testId, int questionId)
        {
            repository.RemoveQuestionFromTest(questionId, testId);
            return RedirectToAction(nameof(ManageTestQuestions), new { testId = testId });
        }

        [Route("Admin/Test/{testId}/Import")]
        public IActionResult Import(int testId)
        {
            var viewModel = new ImportVM()
            {
                TestId = testId
            };

            return View(viewModel);
        }


        [HttpPost]
        public IActionResult CopyQuestionsToTest(int testId, int[] questionIds)
        {
            //TODO: multiple questions in one query
            foreach (var qId in questionIds)
                repository.CopyQuestionToTest(qId, testId, User.Identity.Name);

            return Json(repository.GetCurrentTestImportData(testId));
        }

        [HttpPost]
        public IActionResult DeleteQuestionsFromTest(int testId, int[] questionIds)
        {
            //TODO: multiple questions in one query
            foreach (var qId in questionIds)
                repository.RemoveQuestionFromTest(qId, testId);

            return Json(repository.GetCurrentTestImportData(testId));
        }

        public ActionResult PreviewQuestionPartial(int id)
        {
            QuestionFormVM viewModelPartial = repository.GetPreviewQuestionPartial(id);

            return PartialView("_PreviewQuestionFormPartial", viewModelPartial);
        }

        public IActionResult GetImportData(int id)
        {
            var viewModel = new
            {
                allTestsData = repository.GetAllTestsImportData(id),
                currentTestData = repository.GetCurrentTestImportData(id)
            };

            return Json(viewModel);
        }

        
        //object GetAllTestsImportData(int currentTestId)
        //{
        //    var allTests = repository.GetAllTests();

        //    var allTestsData = allTests.Where(t => t.Id != currentTestId).Select(o => new
        //    {
        //        text = o.Name,
        //        children = o.Questions.Select(q => new
        //        {
        //            id = $"{AppConstants.Import_QuestionIdPrefix}{q.Id}",
        //            text = q.QuestionText.Replace("<iframe", "|FRAME|").Replace("<img", "|IMAGE|").Replace("src", "|SOURCE|"),
        //            children = q.Answers.Select(a => new
        //            {
        //                text = $"{a.AnswerText} {(a.IsCorrect ? " (Correct)" : string.Empty)}",
        //                state = new { disabled = true }
        //            })
        //        }),
        //    }).ToArray();

        //    return allTestsData;
        //}

        //object GetCurrentTestImportData(int id)
        //{
        //    var allTests = repository.GetAllTests();

        //    var thisTestData = allTests.Where(o => o.Id == id).Select(o => new
        //    {
        //        text = o.Name,
        //        children = o.Questions.Select(q => new
        //        {
        //            id = $"{AppConstants.Import_QuestionIdPrefix}{q.Id}",
        //            text = q.QuestionText.Replace("<iframe", "|FRAME|").Replace("<img", "|IMAGE|").Replace("src", "|SOURCE|"),
        //            children = q.Answers.Select(a => new
        //            {
        //                text = $"{a.AnswerText} {(a.IsCorrect ? " (Correct)" : string.Empty)}",
        //                state = new { disabled = true }
        //            })
        //        }),
        //    }).Single();
        //    return thisTestData;
        //}

       
    }
}
