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

namespace FormClick.Controllers{
    [Authorize]
    [Route("api/[controller]")]
    public class TemplateController : Controller{
        private readonly AppDBContext _appDbContext;
        private readonly ILogger<TemplateController> _logger;

        public TemplateController(AppDBContext appDbContext, ILogger<TemplateController> logger){
            _appDbContext = appDbContext;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index(){
            return View();
        }

        //AQUI PODRIA FALTAR LA VALIDACION PARA QUE SE CAMBIE A UNA CARPETA DEFINITIVA Y DEJE DE ESTAR EN LA TEMPORAL
        [HttpPost("CreateTemplate")]
        public async Task<IActionResult> CreateTemplate([FromBody] TemplateRequest templateRequest) {
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

            Template template = new Template {
                UserId = userId,
                Title = title,
                Description = description,
                Topic = topic,
                Public = isPublic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                picture = imageUrl
            };

            await _appDbContext.Templates.AddAsync(template);
            await _appDbContext.SaveChangesAsync();

            if (!isPublic) {
                List<TemplateAccess> templateAccessList = new List<TemplateAccess>();
                foreach (var acc in templateRequest.SelectedUsers) {
                    bool isValid = int.TryParse(acc, out var accessId);

                    if (!isValid || accessId == 0)
                        continue;

                    TemplateAccess templateAccess = new TemplateAccess {
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

            foreach (var quest in quests) {
                if (string.IsNullOrEmpty(quest.Title) || string.IsNullOrEmpty(quest.Type))
                    return BadRequest(new { message = "Faltan datos en las preguntas." });

                if (quest.Type == "multiple-choice" && (quest.Options == null || !quest.Options.Any()))
                    return BadRequest(new { message = $"La pregunta '{quest.Title}' no tiene opciones válidas." });

                Question question = new Question {
                    TemplateId = template.Id,
                    QuestionType = quest.Type,
                    Text = quest.Title,
                    openAnswer = quest.ExpectedAnswer,
                    IsVisibleInResults = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                await _appDbContext.Questions.AddAsync(question);
                await _appDbContext.SaveChangesAsync();

                if (quest.Type == "multiple-choice") {
                    foreach (var opt in quest.Options) {
                        bool isCorrect = opt == quest.CorrectAnswer;
                        QuestionOption questOpt = new QuestionOption {
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

        //SE MUESTRAN LAS RESPUESTAS DEL TEMPLATE SELECCIONADO
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

        //FUNCION PARA GUARDAR LAS RESPUESTAS
        [HttpPost("StoreAnswer")]
        public async Task<IActionResult> StoreAnswer([FromBody] Dictionary<string, QuestionAnswer> answers){
            int index = 0;
            int correctRespones = 0;
            Response resp = null;
            int templateId = 0;

            foreach (var answer in answers){
                string questionId = answer.Key;
                int parsedQuestionId = int.TryParse(questionId, out var id) ? id : 0;
                string questionType = answer.Value.Type;
                string response = answer.Value.Answer;
                var question = await _appDbContext.Questions
                    .Where(q => q.Id == parsedQuestionId && q.DeletedAt == null)
                    .FirstOrDefaultAsync();

                bool Iscorrect = false;

                if (index == 0){
                    index++;
                    templateId = question.TemplateId;
                    resp = new Response {
                        TemplateId = templateId,
                        UserId = answer.Value.UserId,
                        Score = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };

                    if (resp != null){
                        await _appDbContext.Responses.AddRangeAsync(resp);
                        await _appDbContext.SaveChangesAsync();
                    }
                }

                if (questionType == "trueFalse"){
                    if (question.openAnswer == response){
                        Iscorrect = true;
                        correctRespones++;
                    }

                    Answer answ = new Answer{
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

                }else if (questionType == "MultipleChoice"){
                    int parsedOption = int.TryParse(response, out var idParsed) ? idParsed : 0;
                    var questionOption = await _appDbContext.QuestionOptions
                        .Where(q => q.QuestionId == parsedQuestionId && q.DeletedAt == null)
                        .Where(q => q.Id == parsedOption)
                        .FirstOrDefaultAsync();

                    if (questionOption != null && questionOption.IsCorrect){
                        Iscorrect = true;
                        correctRespones++;
                    }

                    Answer answ = new Answer{
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

                } else if (questionType == "open") {
                    if (question.openAnswer == response){
                        Iscorrect = true;
                        correctRespones++;
                    }

                    Answer answ = new Answer{
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

            if (resp != null) {
                resp.Score = (float)score;
                resp.UpdatedAt = DateTime.UtcNow;

                await _appDbContext.SaveChangesAsync();
            }

            return Ok(new { message = "Plantilla creada con éxito" });
        }

        [HttpGet("EditTemplate/{templateId}")]
        public async Task<IActionResult> EditTemplate(int templateId){
            var template = await _appDbContext.Templates
                .Where(t => t.Id == templateId)
                .Include(u => u.User)
                .Include(t => t.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            //Console.Write(template);

            return View(template);
        }

        [HttpPost("Update")]
        public async Task<IActionResult> Update([FromBody] Dictionary<string, QuestionAnswer> answers){

            return View();
        }


        //Se muestra la respuesta del template del usuario logeado
        [HttpGet("AnswerTemplate/{templateId}")]
        public async Task<IActionResult> AnswerTemplate(int templateId){
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
        
            var model = new ResponseVM {
                Title = template.Title,
                Description = template.Description,
                Topic = template.Topic,
                Score = response.Score,
                Questions = questions.Select(q => new QuestionVM {
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

        //OBTENER LISTA DE USUARIOS PARA MOSTRAR EN LOS PERMISOS
        [HttpGet("getUsers")]
        public async Task<IActionResult> getUsers(){
            var userList = _appDbContext.Users.Where(t => t.DeletedAt == null).OrderByDescending(t => t.CreatedAt)
                .Select(t => new {
                    Id = t.Id,
                    Email = t.Email
                }).ToList();

            return Json(userList);
        }

        //Se muestran los templates que haz contestado
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



        //Se muestran los templates que haz creado
        [HttpGet("showOwnTemplates")]
        public async Task<IActionResult> showOwnTemplates() {
            var claimsIdentity = User.Identity as System.Security.Claims.ClaimsIdentity;
            var userIdClaim = claimsIdentity?.FindFirst("Id")?.Value;
            int userId = int.TryParse(userIdClaim, out var idParsed) ? idParsed : 0;

            var templates = await _appDbContext.Templates
                .Where(q => q.UserId == userId && q.DeletedAt == null)
                .OrderByDescending(q => q.CreatedAt)
                .Include(q => q.Responses)
                .ThenInclude(r => r.User)
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
                TotalLikes = t.Likes?.Count() ?? 0,
                Responses = t.Responses?.Select(r => new ResponsedBYVM {
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



        //SE MUESTRA LA RESPUESTA DE OTROS USUARIOS A TU TEMPLATE CREADO.
        [HttpGet("showOwnAnsweredTemplates/{responseId}")]
        public async Task<IActionResult> showOwnAnsweredTemplates(int responseId) {
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
                        }).ToList()
                        : null,

                    SelectedAnswer = answers.FirstOrDefault(a => a.QuestionId == q.Id)?.ResponseText,
                    SelectedOptionId = answers.FirstOrDefault(a => a.QuestionId == q.Id)?.OptionId,

                    IsCorrect = GetIsCorrect(q, answers.FirstOrDefault(a => a.QuestionId == q.Id))
                }).ToList()
            };

            return View(model);
        }

        [HttpPost("UploadProfilePicture")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file) {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No se recibió ningún archivo." });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { success = false, message = "Formato de archivo no permitido." });

            // Validar el tamaño del archivo (ejemplo: máximo 5 MB)
            //long maxFileSize = 5 * 1024 * 1024; // 5 MB
            //if (file.Length > maxFileSize)
            //    return BadRequest(new { success = false, message = "El archivo es demasiado grande. Máximo 5 MB." });

            var fileName = $"{Guid.NewGuid()}{extension}";

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/temporaryFiles", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/temporaryFiles/{fileName}";

            return Ok(new { success = true, filePath = fileUrl });
        }

        //Funcion para validar las respuestas correctas
        private bool GetIsCorrect(Question question, Answer userAnswer){
            if (userAnswer == null)
                return false;

            bool isCorrect = false;

            if (question.QuestionType == "open"){
                isCorrect = question.openAnswer == userAnswer.ResponseText;
                Console.Write(isCorrect);
            }

            if (question.QuestionType == "true-false"){
                isCorrect = question.openAnswer == userAnswer.ResponseText;
            }

            if (question.QuestionType == "multiple-choice"){
                if (userAnswer.OptionId.HasValue){
                    var selectedOption = _appDbContext.QuestionOptions
                        .Where(qo => qo.Id == userAnswer.OptionId.Value)
                        .FirstOrDefault();

                    if (selectedOption != null && selectedOption.IsCorrect){
                        isCorrect = true;
                    }
                }
            }

            return isCorrect;
        }

        //------------------------------------------------------------------
        //------------------------------------------------------------------
    }

    public class QuestionAnswer{
        public string Type { get; set; }
        public string Answer { get; set; }
        public int UserId { get; set; }
    }

    public class TemplateRequest{
        public string Title { get; set; }
        public string Topic { get; set; }
        public string Description { get; set; }
        public bool IsPublic { get; set; }
        public string picture { get; set; }
        public List<Quest> Quests { get; set; }
        public List<string> SelectedUsers { get; set; }
    }

    public class Quest{
        public string Title { get; set; }
        public string Type { get; set; }
        public List<string> Options { get; set; }
        public string CorrectAnswer { get; set; }
        public string ExpectedAnswer { get; set; }
    }
    public class SearchRequest {
        public string SearchTerm { get; set; }
    }
}
