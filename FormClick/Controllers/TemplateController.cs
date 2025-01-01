using Microsoft.AspNetCore.Mvc;
using FormClick.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using FormClick.Models;
using Microsoft.AspNetCore.Identity;
using System;
using FormClick.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using static FormClick.Controllers.AccessController;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Net.Http.Json;
using Newtonsoft.Json;
using System.Runtime.ConstrainedExecution;

namespace FormClick.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TemplateController : Controller
    {
        private readonly AppDBContext _appDbContext;
        private readonly ILogger<TemplateController> _logger;
        private readonly S3Service _s3Service;

        public TemplateController(AppDBContext appDbContext, ILogger<TemplateController> logger, S3Service s3Service)
        {
            _appDbContext = appDbContext;
            _logger = logger;
            _s3Service = new S3Service();
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("CreateTemplate")]
        public async Task<IActionResult> CreateTemplate([FromBody] TemplateRequest templateRequest)
        {
            if (templateRequest == null)
                return BadRequest(new { message = "La solicitud es inválida." });

            string title = templateRequest.Title;
            string topic = templateRequest.Topic;
            string description = templateRequest.Description;
            bool isPublic = templateRequest.IsPublic;
            List<Quest> quests = templateRequest.Quests;
            string imageUrl = templateRequest.picture;

            var userClaims = User.Identity as ClaimsIdentity;
            var userIdClaim = userClaims?.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { message = "No se pudo autenticar al usuario." });

            int userId = int.TryParse(userIdClaim, out var id) ? id : 0;
            if (userId == 0)
                return Unauthorized(new { message = "El Id del usuario no es válido." });

            Template template = new Template
            {
                UserId = userId,
                Title = title,
                Description = description,
                Topic = topic,
                Public = isPublic,
                picture = imageUrl,
                Version = 0,
                IsCurrent = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await _appDbContext.Templates.AddAsync(template);
            await _appDbContext.SaveChangesAsync();

            if (!isPublic)
            {
                List<TemplateAccess> templateAccessList = new List<TemplateAccess>();
                foreach (var acc in templateRequest.SelectedUsers)
                {
                    bool isValid = int.TryParse(acc, out var accessId);

                    if (!isValid || accessId == 0)
                        continue;

                    TemplateAccess templateAccess = new TemplateAccess
                    {
                        TemplateId = template.Id,
                        UserId = accessId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };

                    templateAccessList.Add(templateAccess);
                }

                await _appDbContext.TemplateAccess.AddRangeAsync(templateAccessList);
                await _appDbContext.SaveChangesAsync();
            }

            foreach (var quest in quests)
            {
                if (string.IsNullOrEmpty(quest.Title) || string.IsNullOrEmpty(quest.Type))
                    return BadRequest(new { message = "Faltan datos en las preguntas." });

                if (quest.Type == "multiple-choice" && (quest.Options == null || !quest.Options.Any()))
                    return BadRequest(new { message = $"La pregunta '{quest.Title}' no tiene opciones válidas." });

                Question question = new Question
                {
                    TemplateId = template.Id,
                    QuestionType = quest.Type,
                    Text = quest.Title,
                    IsVisibleInResults = false,
                    openAnswer = quest.ExpectedAnswer,
                    templateVersion = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                await _appDbContext.Questions.AddAsync(question);
                await _appDbContext.SaveChangesAsync();

                if (quest.Type == "multiple-choice")
                {
                    foreach (var opt in quest.Options)
                    {
                        bool isCorrect = opt == quest.CorrectAnswer;
                        QuestionOption questOpt = new QuestionOption
                        {
                            QuestionId = question.Id,
                            OptionText = opt,
                            IsCorrect = isCorrect,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                        };

                        await _appDbContext.QuestionOptions.AddAsync(questOpt);
                        await _appDbContext.SaveChangesAsync();
                    }
                }
            }

            return Ok(new { message = "Plantilla creada con éxito" });
        }

        [HttpGet("GetRespond/{templateId}")]
        public async Task<IActionResult> GetRespond(int templateId)
        {
            var template = await _appDbContext.Templates
                .Include(t => t.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (template == null)
                return NotFound(new { message = "Template no encontrado." });

            var viewModel = new TemplateVM
            {
                TemplateId = template.Id,
                Title = template.Title,
                Description = template.Description,
                Topic = template.Topic,
                IsPublic = template.Public,
                Questions = template.Questions.Select(q => new QuestionViewModel
                {
                    QuestionId = q.Id,
                    Text = q.Text,
                    Type = q.QuestionType,
                    Options = q.Options != null ? q.Options.Select(o => new OptionViewModel
                    {
                        OptionId = o.Id,
                        Text = o.OptionText,
                        IsCorrect = o.IsCorrect
                    }).ToList() : new List<OptionViewModel>()
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost("StoreAnswer")]
        public async Task<IActionResult> StoreAnswer([FromBody] Dictionary<string, QuestionAnswer> answers)
        {
            Console.Write(answers);
            try
            {
                int index = 0;
                int correctRespones = 0;
                Response resp = null;

                foreach (var answer in answers)
                {
                    string questionId = answer.Key;
                    int parsedQuestionId = int.TryParse(questionId, out var id) ? id : 0;
                    string questionType = answer.Value.Type;
                    string response = answer.Value.Answer;
                    int templateId = answer.Value.TemplateId;

                    var question = await _appDbContext.Questions
                        .Where(q => q.Id == parsedQuestionId && q.DeletedAt == null)
                        .FirstOrDefaultAsync();

                    var template = _appDbContext.Templates.FirstOrDefault(t => t.Id == templateId);
                    if (template == null)
                        return BadRequest("Something has gone wrong with your account");

                    bool Iscorrect = false;

                    if (index == 0)
                    {
                        index++;
                        templateId = question.TemplateId;
                        resp = new Response
                        {
                            TemplateId = templateId,
                            UserId = answer.Value.UserId,
                            Score = 0,
                            templateVersion = template.Version,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                        };

                        if (resp != null)
                        {
                            await _appDbContext.Responses.AddRangeAsync(resp);
                            await _appDbContext.SaveChangesAsync();
                        }
                    }

                    if (questionType == "trueFalse")
                    {
                        if (question.openAnswer == response)
                        {
                            Iscorrect = true;
                            correctRespones++;
                        }

                        Answer answ = new Answer
                        {
                            QuestionId = question.Id,
                            ResponseText = response,
                            OptionId = null,
                            IsCorrect = Iscorrect,
                            ResponseId = resp.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                        };

                        await _appDbContext.Answers.AddAsync(answ);
                        await _appDbContext.SaveChangesAsync();

                    }
                    else if (questionType == "MultipleChoice")
                    {
                        int parsedOption = int.TryParse(response, out var idParsed) ? idParsed : 0;
                        var questionOption = await _appDbContext.QuestionOptions
                            .Where(q => q.QuestionId == parsedQuestionId && q.DeletedAt == null)
                            .Where(q => q.Id == parsedOption)
                            .FirstOrDefaultAsync();

                        if (questionOption != null && questionOption.IsCorrect)
                        {
                            Iscorrect = true;
                            correctRespones++;
                        }

                        Answer answ = new Answer
                        {
                            QuestionId = question.Id,
                            ResponseText = null,
                            OptionId = parsedOption,
                            IsCorrect = Iscorrect,
                            ResponseId = resp.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                        };

                        await _appDbContext.Answers.AddAsync(answ);
                        await _appDbContext.SaveChangesAsync();

                    }
                    else if (questionType == "open")
                    {
                        if (question.openAnswer == response)
                        {
                            Iscorrect = true;
                            correctRespones++;
                        }

                        Answer answ = new Answer
                        {
                            QuestionId = question.Id,
                            ResponseText = response,
                            OptionId = null,
                            IsCorrect = Iscorrect,
                            ResponseId = resp.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                        };

                        await _appDbContext.Answers.AddAsync(answ);
                        await _appDbContext.SaveChangesAsync();
                    }
                }

                int totalQuestions = answers.Count;
                double score = (double)correctRespones / totalQuestions * 100;

                if (resp != null)
                {
                    resp.Score = (float)score;
                    resp.UpdatedAt = DateTime.UtcNow;

                    await _appDbContext.SaveChangesAsync();
                }

                return Ok(new { message = "Respuestas enviadas exitosamente" });
            }catch (Exception ex){
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                else
                    Console.WriteLine($"Error: {ex.Message}");

                Console.Write("ERROR", ex.Message);

                return StatusCode(500, new { success = false, message = "Error al cargar el archivo", error = ex.Message });
            }
        }

        [HttpGet("EditTemplate/{templateId}")]
        public async Task<IActionResult> EditTemplate(int templateId)
        {
            var template = await _appDbContext.Templates
                .Where(t => t.Id == templateId )
                .Include(u => u.User)
                .Include(t => t.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync();

            if (template == null)
                return NotFound();

            var editTemplateVM = new EditTemplateVM{
                Id = template.Id,
                Title = template.Title,
                picture = template.picture,
                Version = template.Version,
                Description = template.Description,
                Topic = template.Topic,
                Questions = template.Questions.Where(q => q.templateVersion == template.Version).Select(q => new QuestionViewModelEdit {
                    Id = q.Id,
                    Text = q.Text,
                    QuestionType = q.QuestionType,
                    openAnswer = q.openAnswer,
                    Options = q.Options?.Select(o => new OptionViewModelEdit {
                        Id = o.Id,
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect,
                    }).ToList() ?? new List<OptionViewModelEdit>()
                }).ToList()
            };

            return View(editTemplateVM);
        }

        [HttpPost("TemplateUpdate")]
        public async Task<IActionResult> TemplateUpdate([FromBody] TemplateRequestEditVM request) {
            try {
                if (request == null)
                    return BadRequest(new { message = "La solicitud es inválida." });

                var templateId = request.templateId;
                var tittle = request.tittle;
                var description = request.description;
                var topic = request.topic;
                var questions = request.Questions;

                var template = _appDbContext.Templates.FirstOrDefault(t => t.Id == templateId);
                if (template == null)
                    return BadRequest("Something has gone wrong with your account");

                var currentVersion = template.Version;
                TemplateHistorial templateHistorial = new TemplateHistorial {
                    TemplateId = template.Id,
                    UserId = template.UserId,
                    Title = template.Title,
                    Description = template.Description,
                    Topic = template.Topic,
                    Public = template.Public,
                    Version = currentVersion,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Picture = template.picture,
                };

                await _appDbContext.TemplateHistorials.AddAsync(templateHistorial);
                await _appDbContext.SaveChangesAsync();

                template.IsCurrent = false;
                await _appDbContext.SaveChangesAsync();

                Template templateCreate = new Template {
                    UserId = template.UserId,
                    Title = tittle,
                    Description = description,
                    Topic = topic,
                    Public = template.Public,
                    Version = currentVersion + 1,
                    picture = template.picture,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsCurrent = true
                };

                await _appDbContext.Templates.AddAsync(templateCreate);
                await _appDbContext.SaveChangesAsync();

                foreach (var quest in questions) {
                    var questionTitle = quest.Value[0].questionTitle;
                    var questionType = quest.Value[0].questionType;
                    var expectedAnswer = "";
                    var options = quest.Value[0].options;

                    if (questionType == "open")
                        expectedAnswer = quest.Value[0].options[0].optionText;
                    else if(questionType == "true-false") {
                        foreach (var Tf in options) {
                            if (Tf.isCorrect == true){
                                expectedAnswer = Tf.optionId;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(questionTitle) || string.IsNullOrEmpty(questionType))
                        return BadRequest(new { message = "Faltan datos en las preguntas." });

                    if (questionType == "multiple-choice" && (options == null || !options.Any()))
                        return BadRequest(new { message = $"La pregunta '{questionTitle}' no tiene opciones válidas." });

                    Question question = new Question{
                        TemplateId = templateCreate.Id,
                        QuestionType = questionType,
                        Text = questionTitle,
                        openAnswer = expectedAnswer,
                        IsVisibleInResults = false,
                        templateVersion = currentVersion + 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };

                    await _appDbContext.Questions.AddAsync(question);
                    await _appDbContext.SaveChangesAsync();

                    if (questionType == "multiple-choice") {
                        foreach (var opt in options) {
                            bool isCorrect = opt.isCorrect;

                            QuestionOption questOpt = new QuestionOption{
                                QuestionId = question.Id,
                                OptionText = opt.optionId,
                                IsCorrect = isCorrect,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                            };

                            await _appDbContext.QuestionOptions.AddAsync(questOpt);
                            await _appDbContext.SaveChangesAsync();
                        }
                    }
                }

                return Ok(new { message = "Template actualizado correctamente" });
            }catch (Exception ex){

                if (ex.InnerException != null)
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                else
                    Console.WriteLine($"Error: {ex.Message}");

                return StatusCode(500, new { success = false, message = "Error al cargar el archivo", error = ex.Message });
            }

        }

        [HttpGet("AnswerTemplate/{templateId}")]
        public async Task<IActionResult> AnswerTemplate(int templateId)
        {
            var template = await _appDbContext.Templates.Where(t => t.Id == templateId).FirstOrDefaultAsync();

            var claimsIdentity = User.Identity as System.Security.Claims.ClaimsIdentity;
            var userIdClaim = claimsIdentity?.FindFirst("Id")?.Value;
            int userId = int.TryParse(userIdClaim, out var idParsed) ? idParsed : 0;

            var response = await _appDbContext.Responses.Where(r => r.TemplateId == templateId)
                .Where(r => r.UserId == userId)
                .FirstOrDefaultAsync();

            var questions = await _appDbContext.Questions.Where(q => q.TemplateId == templateId && q.DeletedAt == null)
                .Include(q => q.Options).ToListAsync();

            var answers = await _appDbContext.Answers.Where(a => a.Response.UserId == userId).Where(a => a.Question.TemplateId == templateId)
                .Include(a => a.Question).Include(a => a.Option)
                .ToListAsync();

            var model = new ResponseVM
            {
                Title = template.Title,
                Description = template.Description,
                Topic = template.Topic,
                Score = response.Score,
                Questions = questions.Select(q => new QuestionVM
                {
                    QuestionId = q.Id,
                    Text = q.Text,
                    Type = q.QuestionType,
                    Options = q.QuestionType == "multiple-choice"
                        ? q.Options.Select(o => new OptionVM
                        {
                            OptionId = o.Id,
                            Text = o.OptionText
                        }).ToList()
                        : null,

                    SelectedAnswer = answers.FirstOrDefault(a => a.QuestionId == q.Id)?.ResponseText,
                    SelectedOptionId = answers.FirstOrDefault(a => a.QuestionId == q.Id)?.OptionId,

                    IsCorrect = GetIsCorrect(q, answers.FirstOrDefault(a => a.QuestionId == q.Id))
                }).ToList()
            };

            return View(model);
        }

        [HttpGet("getUsers")]
        public async Task<IActionResult> getUsers()
        {
            var userList = _appDbContext.Users.Where(t => t.DeletedAt == null).OrderByDescending(t => t.CreatedAt)
                .Select(t => new {
                    Id = t.Id,
                    Email = t.Email
                }).ToList();

            return Json(userList);
        }

        [HttpGet("showAnsweredTemplates")]
        public async Task<IActionResult> ShowAnsweredTemplates()
        {
            var claimsIdentity = User.Identity as System.Security.Claims.ClaimsIdentity;
            var userIdClaim = claimsIdentity?.FindFirst("Id")?.Value;
            int userId = int.TryParse(userIdClaim, out var idParsed) ? idParsed : 0;

            // Obtener las respuestas desde la base de datos
            var responses = await _appDbContext.Responses
                .Where(q => q.UserId == userId)
                .Where(q => q.DeletedAt == null)
                .OrderByDescending(q => q.CreatedAt)
                .Include(q => q.Answers)
                .Include(q => q.Template)
                .ThenInclude(t => t.User)
                .ToListAsync();

            var defaultValue = "";
            // Transformar las respuestas a ViewModels
            var responseViewModels = responses.Select(response => new ResponseViewModel
            {
                ResponseId = response.Id,
                TemplateId = response.Template.Id,
                TemplateName = response.Template?.Title ?? defaultValue,
                Description = response.Template?.Description ?? defaultValue,
                Topic = response.Template?.Topic ?? defaultValue,
                picture = response.Template?.picture ?? defaultValue,
                userPicture = response.Template?.User?.ProfilePicture ?? defaultValue,
                userName = response.Template?.User?.Username ?? defaultValue,
                Score = response.Score,
                CreatedAt = response.Template?.CreatedAt ?? DateTime.MinValue,
                Answers = response.Answers.Select(answer => new AnswerViewModel
                {
                    AnswerId = answer.Id,
                    QuestionId = answer.QuestionId,
                    ResponseText = answer?.ResponseText,
                    OptionId = answer.OptionId,
                    ResponseId = answer.ResponseId,
                    IsCorrect = answer.IsCorrect,
                }).ToList()
            }).ToList();

            // Pasar el ViewModel a la vista
            return View(responseViewModels);
        }

        [HttpGet("showOwnTemplates")]
        public async Task<IActionResult> showOwnTemplates() {
            var claimsIdentity = User.Identity as System.Security.Claims.ClaimsIdentity;
            var userIdClaim = claimsIdentity?.FindFirst("Id")?.Value;
            int userId = int.TryParse(userIdClaim, out var idParsed) ? idParsed : 0;

            var templates = await _appDbContext.Templates
                .Where(q => q.UserId == userId && q.DeletedAt == null)
                .OrderByDescending(q => q.CreatedAt)
                .Include(t => t.Questions)
                .Include(q => q.Responses)
                .Include(r => r.User)
                .Include(q => q.Likes)
                .ToListAsync();

            var answerTemplateVMs = templates.Select(t => new AnswerTemplateVM {
                TemplateId = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                UserId = userId,
                UserName = t.User.Username,
                ProfilePicture = t.User.ProfilePicture,
                picture = t.picture,
                Topic = t.Topic,
                Version = t.Version,
                IsCurrent = t.IsCurrent,
                TotalLikes = t.Likes?.Count() ?? 0,
                Responses = t.Responses?.Where(rr => rr.templateVersion == t.Version).Select(r => new ResponsedBYVM {
                    ResponseId = r.Id,
                    UserId = r.User?.Id ?? 0,
                    UserName = r.User?.Username,
                    ProfilePicture = r.User?.ProfilePicture,
                    Score = r.Score,
                    CreatedAt = r.CreatedAt
                }).ToList() ?? new List<ResponsedBYVM>()
            }).ToList();

            return View(answerTemplateVMs);
        }

        [HttpGet("showOwnAnsweredTemplates/{responseId}")]
        public async Task<IActionResult> showOwnAnsweredTemplates(int responseId)
        {
            var response = await _appDbContext.Responses.Where(t => t.Id == responseId).Include(t => t.User).FirstOrDefaultAsync();
            var template = await _appDbContext.Templates.Where(t => t.Id == response.TemplateId).FirstOrDefaultAsync();
            var questions = await _appDbContext.Questions.Where(q => q.TemplateId == template.Id && q.DeletedAt == null).Include(q => q.Options).ToListAsync();

            var answers = await _appDbContext.Answers.Where(a => a.Response.UserId == response.UserId)
                .Where(a => a.Question.TemplateId == template.Id)
                .Include(a => a.Question)
                .Include(a => a.Option)
                //.ThenInclude(a => a.User)
                .ToListAsync();


            var model = new ResponseVM
            {
                Title = template.Title,
                Description = template.Description,
                Topic = template.Topic,
                Score = response.Score,
                Username = response.User.Username,
                Questions = questions.Select(q => new QuestionVM
                {
                    QuestionId = q.Id,
                    Text = q.Text,
                    Type = q.QuestionType,
                    Options = q.QuestionType == "multiple-choice"
                        ? q.Options.Select(o => new OptionVM
                        {
                            OptionId = o.Id,
                            Text = o.OptionText
                        }).ToList() : null,

                    SelectedAnswer = answers.FirstOrDefault(a => a.QuestionId == q.Id)?.ResponseText,
                    SelectedOptionId = answers.FirstOrDefault(a => a.QuestionId == q.Id)?.OptionId,

                    IsCorrect = GetIsCorrect(q, answers.FirstOrDefault(a => a.QuestionId == q.Id))
                }).ToList()
            };

            return View(model);
        }

        [HttpPost("loadFile")]
        public async Task<IActionResult> loadFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File not selected");

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var contentType = file.ContentType;

            using var stream = file.OpenReadStream();

            var fileUrl = await _s3Service.UploadFileAsync(stream, fileName, contentType, "ImageTemplate");

            return Json(new { success = true, filePath = fileUrl, subio = "asas" });
        }

        [HttpPost("UploadFile/{templateId}")]
        public async Task<IActionResult> UploadFile([FromRoute] int templateId, [FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("File not selected");

                var template = _appDbContext.Templates.FirstOrDefault(t => t.Id == templateId);
                if (template == null)
                    return BadRequest("Something has gone wrong with your account");

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var contentType = file.ContentType;
                using var stream = file.OpenReadStream();

                var fileUrl = await _s3Service.UploadFileAsync(stream, fileName, contentType, "ImageTemplate");

                template.picture = fileUrl;
                _appDbContext.SaveChanges();

                return Json(new { success = true, filePath = fileUrl, templateId = templateId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error al cargar el archivo", error = ex.Message });
            }
        }

        private bool GetIsCorrect(Question question, Answer userAnswer)
        {
            if (userAnswer == null)
                return false;

            bool isCorrect = false;

            if (question.QuestionType == "open")
            {
                isCorrect = question.openAnswer == userAnswer.ResponseText;
                Console.Write(isCorrect);
            }

            if (question.QuestionType == "true-false")
            {
                isCorrect = question.openAnswer == userAnswer.ResponseText;
            }

            if (question.QuestionType == "multiple-choice")
            {
                if (userAnswer.OptionId.HasValue)
                {
                    var selectedOption = _appDbContext.QuestionOptions
                        .Where(qo => qo.Id == userAnswer.OptionId.Value)
                        .FirstOrDefault();

                    if (selectedOption != null && selectedOption.IsCorrect)
                    {
                        isCorrect = true;
                    }
                }
            }

            return isCorrect;
        }
    }

    public class QuestionAnswer
    {
        public string Type { get; set; }
        public string Answer { get; set; }
        public int UserId { get; set; }
        public int TemplateId { get; set; }
    }
    public class TemplateRequest
    {
        public string Title { get; set; }
        public string Topic { get; set; }
        public string Description { get; set; }
        public bool IsPublic { get; set; }
        public string picture { get; set; }
        public List<Quest> Quests { get; set; }
        public List<string> SelectedUsers { get; set; }
    }
    public class Quest
    {
        public string Title { get; set; }
        public string Type { get; set; }
        public List<string> Options { get; set; }
        public string CorrectAnswer { get; set; }
        public string ExpectedAnswer { get; set; }
    }
    public class SearchRequest
    {
        public string SearchTerm { get; set; }
    }
    public class TemplateRequestEditVM
    {
        public string description { get; set; }
        public int templateId { get; set; }
        public string tittle { get; set; }
        public string topic { get; set; }
        public Dictionary<string, List<QuestionEditVM>> Questions { get; set; }
    }
    public class QuestionEditVM
    {
        public string questionTitle { get; set; }
        public string questionType { get; set; }
        public List<OptionEditVM> options { get; set; }
        public string? expectedAnswer { get; set; }
    }
    public class OptionEditVM
    {
        public string optionId { get; set; }
        public string optionText { get; set; }
        public bool isCorrect { get; set; }
    }

}
