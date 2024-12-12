using Microsoft.AspNetCore.Mvc;
using FormClick.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using FormClick.Models;
using Microsoft.AspNetCore.Identity;
using System;
using FormClick.ViewModels;

namespace FormClick.Controllers
{
    [Route("api/[controller]")]
    public class TemplateController : Controller
    {
        private readonly AppDBContext _appDbContext;
        private readonly ILogger<TemplateController> _logger;

        public TemplateController(AppDBContext appDbContext, ILogger<TemplateController> logger)
        {
            _appDbContext = appDbContext;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index(){
            return View();
        }

        [HttpPost("CreateTemplate")]
        public async Task<IActionResult> CreateTemplate([FromBody] TemplateRequest templateRequest)
        {
            if (templateRequest == null)
            {
                _logger.LogWarning("La solicitud es inválida. El cuerpo de la solicitud es null.");
                return BadRequest(new { message = "La solicitud es inválida." });
            }

            string title = templateRequest.Title;
            string topic = templateRequest.Topic;
            string description = templateRequest.Description;
            bool isPublic = templateRequest.IsPublic;
            List<Quest> quests = templateRequest.Quests;

            _logger.LogInformation("Iniciando la creación de la plantilla con el título '{Title}'", title);

            var userClaims = User.Identity as ClaimsIdentity;
            var userIdClaim = userClaims?.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogWarning("No se pudo encontrar el Id del usuario en los claims.");
                return Unauthorized(new { message = "No se pudo autenticar al usuario." });
            }

            int userId = int.TryParse(userIdClaim, out var id) ? id : 0;
            if (userId == 0)
            {
                _logger.LogWarning("El Id del usuario no es válido. Valor obtenido: '{UserId}'", userId);
                return Unauthorized(new { message = "El Id del usuario no es válido." });
            }

            Template template = new Template
            {
                UserId = userId,
                Title = title,
                Description = description,
                Topic = topic,
                Public = isPublic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            try
            {
                await _appDbContext.Templates.AddAsync(template);
                await _appDbContext.SaveChangesAsync();
                _logger.LogInformation("Plantilla '{Title}' creada con éxito en la base de datos.", title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar la plantilla '{Title}' en la base de datos.", title);
                return StatusCode(500, new { message = "Ocurrió un error al crear la plantilla." });
            }

            if (!isPublic){
                List<TemplateAccess> templateAccessList = new List<TemplateAccess>();
                foreach (var acc in templateRequest.SelectedUsers) {
                    bool isValid = int.TryParse(acc, out var accessId);
                    if (!isValid || accessId == 0) {
                        _logger.LogWarning("Id de usuario no válido en SelectedUsers: '{SelectedUserId}'", acc);
                        continue;
                    }

                    TemplateAccess templateAccess = new TemplateAccess {
                        TemplateId = template.Id,
                        UserId = accessId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };

                    templateAccessList.Add(templateAccess);
                }

                try {
                    if (templateAccessList.Any()) {
                        await _appDbContext.TemplateAccess.AddRangeAsync(templateAccessList);
                        await _appDbContext.SaveChangesAsync();
                        _logger.LogInformation("Accesos a la plantilla '{Title}' registrados correctamente.", title);
                    }
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al guardar los accesos de la plantilla '{Title}'", title);
                    return StatusCode(500, new { message = "Ocurrió un error al registrar los accesos." });
                }
            }

            foreach (var quest in quests) {
                if (string.IsNullOrEmpty(quest.Title) || string.IsNullOrEmpty(quest.Type)){
                    _logger.LogWarning("Faltan datos en la pregunta: '{QuestTitle}'. Tipo: '{QuestType}'", quest.Title, quest.Type);
                    return BadRequest(new { message = "Faltan datos en las preguntas." });
                }

                if (quest.Type == "multiple-choice" && (quest.Options == null || !quest.Options.Any())){
                    _logger.LogWarning("La pregunta '{QuestTitle}' no tiene opciones válidas.", quest.Title);
                    return BadRequest(new { message = $"La pregunta '{quest.Title}' no tiene opciones válidas." });
                }

                Question question = new Question {
                    TemplateId = template.Id,
                    QuestionType = quest.Type,
                    Text = quest.Title,
                    openAnswer = quest.Type == "open" ? quest.ExpectedAnswer : null,
                    IsVisibleInResults = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                try {
                    await _appDbContext.Questions.AddAsync(question);
                    await _appDbContext.SaveChangesAsync();
                    _logger.LogInformation("Pregunta '{QuestTitle}' creada con éxito.", quest.Title);
                }catch (Exception ex){
                    _logger.LogError(ex, "Error al guardar la pregunta '{QuestTitle}'", quest.Title);
                    return StatusCode(500, new { message = "Ocurrió un error al guardar la pregunta." });
                }

                if (quest.Type == "multiple-choice"){
                    foreach (var opt in quest.Options){
                        bool isCorrect = opt == quest.CorrectAnswer;
                        QuestionOption questOpt = new QuestionOption{
                            QuestionId = question.Id,
                            OptionText = opt,
                            IsCorrect = isCorrect,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                        };

                        try{
                            await _appDbContext.QuestionOptions.AddAsync(questOpt);
                            await _appDbContext.SaveChangesAsync();
                            _logger.LogInformation("Opción de la pregunta '{QuestTitle}' registrada con éxito.", quest.Title);
                        }catch (Exception ex){
                            _logger.LogError(ex, "Error al guardar la opción de la pregunta '{QuestTitle}'", quest.Title);
                            return StatusCode(500, new { message = "Ocurrió un error al guardar la opción de la pregunta." });
                        }
                    }
                }
            }

            _logger.LogInformation($"Plantilla '{title}' creada con éxito.");
            return Ok(new { message = "Plantilla creada con éxito" });
        }

        [HttpGet("getUsers")]
        public async Task<IActionResult> getUsers()
        {
            var userList = _appDbContext.Users
                .Where(t => t.DeletedAt == null)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    Id = t.Id,
                    Email = t.Email
                }).ToList();
  
            return Json(userList);
        }

    }

    public class TemplateRequest
    {
        public string Title { get; set; }
        public string Topic { get; set; }
        public string Description { get; set; }
        public bool IsPublic { get; set; }
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
}
