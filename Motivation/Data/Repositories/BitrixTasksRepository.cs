using Microsoft.EntityFrameworkCore;
using Motivation.Models;
using Motivation.Models.Mobile;

namespace Motivation.Data.Repositories
{
    public class BitrixTasksRepository : BitrixBaseRepository
    {
        private readonly IEmployeesRepository _employeesRepository;
        private readonly IWebHostEnvironment _appEnvironment;

        // placeholder for the case if Author is not connected to bitrix
        public readonly Employee UnknownEmployee = new Employee
        {
            Id = 0,
            Email = "unknown@mail.com",
            FirstName = "Неизвестный",
            LastName = "Неизвестный",
            MiddleName = "Неизвестный",
            Photo = "/images/profile.png",
        };

        public BitrixTasksRepository(
            IConfiguration configuration,
            IEmployeesRepository employeesRepository,
            IWebHostEnvironment appEnvironment
        )
            : base(configuration)
        {
            _employeesRepository = employeesRepository;
            _appEnvironment = appEnvironment;
        }

        public async Task AddCommentToTaskById(
            int taskId,
            int authorId,
            string message,
            IFormFile? photo
        )
        {
            HttpResponseMessage res;
            if (photo is not null)
            {
                using var source = photo.OpenReadStream();
                using var dest = new MemoryStream();
                await source.CopyToAsync(dest);
                var bytes = dest.ToArray();
                var base64 = Convert.ToBase64String(bytes);
                var content = new StringContent(
                    base64,
                    System.Text.Encoding.UTF8,
                    "application/text"
                );
                res = await _httpClient.PostAsync(
                    $"api/tasks/comments/add.php?taskId={taskId}&authorId={authorId}&message={message}&filename={photo.FileName}",
                    content
                );
            }
            else
            {
                res = await _httpClient.PostAsync(
                    $"api/tasks/comments/add.php?taskId={taskId}&authorId={authorId}&message={message}",
                    null
                );
            }

            res.EnsureSuccessStatusCode();
        }

        public async Task<List<BitrixResponseCommentModel>> GetCommentsOfTaskById(int taskId)
        {
            var comments =
                await _httpClient.GetFromJsonAsync<List<BitrixResponseCommentModel>>(
                    $"api/tasks/comments/list.php?taskId={taskId}"
                ) ?? new List<BitrixResponseCommentModel>();

            return comments;
        }

        public async Task<BitrixResponseTaskModel?> GetTaskById(int taskId)
        {
            var task = await _httpClient.GetFromJsonAsync<BitrixResponseTaskModel>(
                $"api/tasks/get.php?taskId={taskId}"
            );

            return task;
        }

        public async Task ChangeTaskStatus(int taskId, Models.TaskStatus status)
        {
            var res = await _httpClient.PutAsync(
                $"api/tasks/update_status.php?taskId={taskId}&status={(int)status}",
                null
            );

            res.EnsureSuccessStatusCode();
        }

        public async Task<List<BitrixResponseTaskModel>> GetTasksByEmployeeAndStatus(
            Employee employee,
            Models.TaskStatus? status
        )
        {
            var tasks =
                await _httpClient.GetFromJsonAsync<List<BitrixResponseTaskModel>>(
                    $"api/tasks/list.php?responsibleId={employee.BitrixUserId}&status={(int)status}"
                ) ?? new List<BitrixResponseTaskModel>();

            return tasks;
        }

        public async Task<List<MobileResponseTaskModel>> TranslateBitrixTasksToMobileTask(
            List<BitrixResponseTaskModel> tasks
        )
        {
            var bitrixUsers = tasks.Select(t => int.Parse(t.CreatedBy)).Distinct().ToArray();
            var employeesUsers = await _employeesRepository
                .Entries.Where(u => bitrixUsers.Contains(u.BitrixUserId))
                .ToListAsync();

            var translatedTasks = tasks
                .Select(t =>
                {
                    var employee =
                        employeesUsers.FirstOrDefault(u => u.BitrixUserId == int.Parse(t.CreatedBy))
                        ?? UnknownEmployee;
                    return MergeEmployeeAndBitrixTask(employee, t);
                })
                .ToList();

            return translatedTasks;
        }

        private Models.TaskStatus TranslateStatusFromBitrixToApp(int status)
        {
            switch (status)
            {
                case 1:
                case 2:
                    return Models.TaskStatus.New;
                case 3:
                case 4:
                    return Models.TaskStatus.InProgress;
                default:
                    return Models.TaskStatus.Finished;
            }
        }

        public MobileResponseTaskModel MergeEmployeeAndBitrixTask(
            Employee employee,
            BitrixResponseTaskModel task
        )
        {
            return new MobileResponseTaskModel
            {
                Id = int.Parse(task.Id),
                DeadLine = task.DeadLine is not null
                    ? DateTime.Parse(task.DeadLine).ToLocalTime().ToString()
                    : "",
                Title = task.Title,
                Description = task.Description,
                Status = TranslateStatusFromBitrixToApp(int.Parse(task.Status)),
                Author = new MobileResponseEmployeeModel
                {
                    Id = employee.Id,
                    Email = employee.Email,
                    FullName = employee.GetShortName(),
                    Photo = employee.Photo,
                },
            };
        }

        public async Task<IEnumerable<MobileResponseCommentModel>> TranslateFromBitrixComment(
            IEnumerable<BitrixResponseCommentModel> comments
        )
        {
            var bitrixUsers = comments.Select(c => c.AuthorId).Distinct().ToArray();
            var employeesUsers = await _employeesRepository
                .Entries.Where(u => bitrixUsers.Contains(u.BitrixUserId))
                .ToListAsync();

            var translatedCommentsJob = comments
                .Select(c =>
                {
                    var employee =
                        employeesUsers.FirstOrDefault(u => u.BitrixUserId == c.AuthorId)
                        ?? UnknownEmployee;
                    return MergeEmployeeAndBitrixComment(employee, c);
                })
                .ToList();

            var translatedComments = await Task.WhenAll(translatedCommentsJob);

            return translatedComments;
        }

        public async Task<MobileResponseCommentModel> MergeEmployeeAndBitrixComment(
            Employee employee,
            BitrixResponseCommentModel comment
        )
        {
            var photo = string.Empty;
            if (comment.FileName != string.Empty)
            {
                var folderPath = $"{_appEnvironment.WebRootPath}/images/comments";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var commentPhotoName = comment.FileName;
                var savePath = $"{folderPath}/{commentPhotoName}.jpg";
                if (!File.Exists(savePath))
                {
                    var bytes = Convert.FromBase64String(comment.File);
                    using var source = new MemoryStream(bytes);
                    using var fileStream = new FileStream(savePath, FileMode.Create);
                    await source.CopyToAsync(fileStream);
                }

                photo = $"/images/comments/{commentPhotoName}.jpg";
                Console.WriteLine("Content root path: " + _appEnvironment.WebRootPath);
                Console.WriteLine($"file: {savePath}, photo: {photo}");
            }

            return new MobileResponseCommentModel
            {
                text = comment.PostMessage,
                photo = photo, // let it be empty, pls
                created = DateTime.Parse(comment.PostDate),
                author = new MobileResponseCommentEmployeeModel
                {
                    name = employee.GetShortName(),
                    photo = employee.Photo,
                },
            };
        }
    }
}
