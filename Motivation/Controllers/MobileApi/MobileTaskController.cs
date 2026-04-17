using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motivation.Data;
using Motivation.Data.Repositories;
using Motivation.Models;
using Motivation.Models.Mobile;
using Newtonsoft.Json;

namespace Motivation.Controllers.MobileApi
{
    [Route("api/v{version:apiVersion}/Tasks")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MobileTaskController : ControllerBase
    {
        private readonly ILogger<MobileTaskController> _logger;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly IRepository<EmployeeTask> _employeeTasksRepository;
        private readonly IRepository<Comment> _commentsRepository;
        private readonly BitrixTasksRepository _bitrixTasksRepository;

        public MobileTaskController(
            ILogger<MobileTaskController> logger,
            IWebHostEnvironment appEnvironment,
            IEmployeesRepository employeesRepository,
            IRepository<EmployeeTask> employeeTasksRepository,
            IRepository<Comment> commentsRepository,
            UserManager<IdentityUser> userManager,
            BitrixTasksRepository bitrixTasksRepository
        )
        {
            _logger = logger;
            _appEnvironment = appEnvironment;
            _employeesRepository = employeesRepository;
            _employeeTasksRepository = employeeTasksRepository;
            _commentsRepository = commentsRepository;
            _userManager = userManager;
            _bitrixTasksRepository = bitrixTasksRepository;
        }

        [HttpGet("{taskId}")]
        public async Task GetEmployeeTask(int taskId)
        {
            try
            {
                var taskJob = _employeeTasksRepository
                    .Entries.Where(t => t.Id == taskId)
                    .FirstOrDefaultAsync();
                var bitrixTaskJob = _bitrixTasksRepository.GetTaskById(taskId);

                await Task.WhenAll(taskJob, bitrixTaskJob);
                var task = taskJob.Result;
                var bitrixTask = bitrixTaskJob.Result;
                if (task is null && bitrixTask is null)
                {
                    Response.StatusCode = StatusCodes.Status404NotFound;
                    await Response.WriteAsync("Task not found");
                    return;
                }

                MobileResponseTaskModel responseTask;
                string? json;
                if (task is not null)
                {
                    responseTask = new MobileResponseTaskModel
                    {
                        Id = task.Id,
                        Title = task.Title,
                        Description = task.Description,
                        DeadLine = task.Deadline?.ToLocalTime().ToString("d"),
                        Status = task.Status,
                        Author = new MobileResponseEmployeeModel
                        {
                            Id = task.Author.Id,
                            FullName = task.Author.GetShortName(),
                            Email = task.Author.Email,
                            Photo = task.Author.Photo,
                        },
                    };

                    json = JsonConvert.SerializeObject(responseTask);
                    await Response.WriteAsync(json);

                    return;
                }

                if (bitrixTask is not null)
                {
                    var employee =
                        await _employeesRepository.Entries.FirstOrDefaultAsync(
                            (u) => u.BitrixUserId == int.Parse(bitrixTask.CreatedBy)
                        ) ?? _bitrixTasksRepository.UnknownEmployee;

                    responseTask = _bitrixTasksRepository.MergeEmployeeAndBitrixTask(
                        employee,
                        bitrixTask
                    );

                    json = JsonConvert.SerializeObject(responseTask);
                    await Response.WriteAsync(json);
                }
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying get task:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpPost("{taskId}/ChangeStatus")]
        public async Task ChangeEmployeeTaskStatus(int taskId, [FromBody] MobileTaskStatus status)
        {
            try
            {
                var task = await _employeeTasksRepository
                    .Entries.Where(t => t.Id == taskId)
                    .FirstOrDefaultAsync();
                if (task != null)
                {
                    task.Status = status.Status;
                    await _employeeTasksRepository.UpdateAsync(task);
                }
                else
                {
                    await _bitrixTasksRepository.ChangeTaskStatus(taskId, status.Status);
                }
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying change task status:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpGet("{taskId}/Comments")]
        public async Task GetCommentsToEmployeeTask(int taskId)
        {
            try
            {
                var responseComments = new List<MobileResponseCommentModel>();
                var comments = await _commentsRepository
                    .Entries.Where(c => c.EmployeeTaskId == taskId)
                    .OrderByDescending(c => c.Created)
                    .ToListAsync();
                var translatedComments = comments.Select(c => new MobileResponseCommentModel
                {
                    author = new MobileResponseCommentEmployeeModel
                    {
                        name = c.Author.GetShortName(),
                        photo = c.Author.Photo,
                    },
                    text = c.Text,
                    photo = c.Photo,
                    created = c.Created.ToLocalTime(),
                });
                responseComments.AddRange(translatedComments);

                try
                {
                    var bitrixComments = await _bitrixTasksRepository.GetCommentsOfTaskById(taskId);
                    var translatedBitrixComments =
                        await _bitrixTasksRepository.TranslateFromBitrixComment(bitrixComments);
                    responseComments.AddRange(
                        translatedBitrixComments.OrderByDescending(c => c.created)
                    );
                }
                catch (Exception e)
                {
                    var exceptionString =
                        $"Error occured while trying to get comments from Bitrix:\n {e}";
                    _logger.LogError(exceptionString);
                }

                var json = JsonConvert.SerializeObject(responseComments);
                await Response.WriteAsync(json);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying get comments:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpPost("{taskId}/Comments")]
        public async Task AddCommentToEmployeeTask(
            int taskId,
            [FromForm] MobileComment mobileComment
        )
        {
            try
            {
                var author = await GetCaller() ?? throw new Exception("Commentator cannot be null");

                var task = await _employeeTasksRepository.Entries.FirstOrDefaultAsync(t =>
                    t.Id == taskId
                );
                if (task is not null)
                {
                    var comment = new Comment
                    {
                        AuthorId = author.Id,
                        EmployeeTaskId = taskId,
                        Text = mobileComment.Text,
                    };

                    var photo = mobileComment.Photo;
                    if (photo != null)
                    {
                        var folderPath = $"{_appEnvironment.WebRootPath}/images/comments";
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        var commentPhotoName = photo.FileName.Replace(' ', '_');
                        var savePath = $"{folderPath}/{commentPhotoName}";
                        using (var fileStream = new FileStream(savePath, FileMode.Create))
                        {
                            await photo.CopyToAsync(fileStream);
                        }
                        comment.Photo = $"/images/comments/{commentPhotoName}";
                    }

                    await _commentsRepository.CreateAsync(comment);
                    return;
                }

                if (author.BitrixUserId != 0)
                {
                    try
                    {
                        await _bitrixTasksRepository.AddCommentToTaskById(
                            taskId,
                            author.BitrixUserId,
                            mobileComment.Text,
                            mobileComment?.Photo
                        );
                    }
                    catch (Exception e)
                    {
                        var exceptionString =
                            $"Error occured while trying create comment for task in Bitrix:\n {e}";
                        _logger.LogError(exceptionString);
                    }
                }
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying create comment for task:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        private async Task<Employee?> GetCaller()
        {
            var userEmail = User.Claims.FirstOrDefault(i => i.Type == ClaimTypes.Name);
            if (userEmail == null)
                return null;

            var user = await _userManager.FindByEmailAsync(userEmail.Value);
            if (user == null)
                return null;

            var employee = await _employeesRepository.Entries.FirstOrDefaultAsync(e =>
                e.UserId == user.Id
            );
            return employee;
        }
    }
}
