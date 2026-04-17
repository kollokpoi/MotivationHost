using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public interface IBitrixSyncService
    {
        Task<BitrixApiResponse?> SyncDepartmentAsync(BitrixPortal portal, Department department, string action);
        Task<BitrixApiResponse?> DeleteDepartmentAsync(BitrixPortal portal, int bitrixDepartmentId);
        
        Task<BitrixApiResponse?> SyncUserAsync(BitrixPortal portal, Employee employee, string action);
        Task<BitrixApiResponse?> DeleteUserAsync(BitrixPortal portal, int bitrixUserId);
        
        Task<BitrixApiResponse?> SyncTaskAsync(BitrixPortal portal, EmployeeTask task, string action);
        Task<BitrixApiResponse?> DeleteTaskAsync(BitrixPortal portal, int bitrixTaskId);
    }

    public class BitrixApiResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int? ExternalId { get; set; }
        public string? RawResponse { get; set; }
    }

    public class BitrixSyncService : IBitrixSyncService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BitrixSyncService> _logger;
        private readonly IFieldMappingService _fieldMappingService;

        public BitrixSyncService(
            HttpClient httpClient, 
            ILogger<BitrixSyncService> logger,
            IFieldMappingService fieldMappingService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _fieldMappingService = fieldMappingService;
        }

        public async Task<BitrixApiResponse?> SyncDepartmentAsync(BitrixPortal portal, Department department, string action)
        {
            try
            {
                // Используем маппинг полей вместо жестко заданных имен
                var fields = await _fieldMappingService.MapToBitrixAsync(portal.Id, department);
                
                if (!fields.Any())
                {
                    _logger.LogWarning($"Нет настроек маппинга для департамента {department.Id}, используем значения по умолчанию");
                    fields = new Dictionary<string, object>
                    {
                        ["NAME"] = department.Name,
                        ["PARENT"] = department.ParentId > 0 ? department.ParentId : null,
                        ["BUDGET"] = department.Budget,
                        ["UF_DEPARTMENT_MANAGER_ID"] = department.ManagerId > 0 ? department.ManagerId : null
                    };
                }

                var payload = new
                {
                    fields,
                    params_action = action
                };

                return await SendRequestAsync(portal.WebhookUrl, "department.item.save", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка синхронизации подразделения {department.Name} с порталом {portal.Name}");
                return new BitrixApiResponse 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }

        public async Task<BitrixApiResponse?> DeleteDepartmentAsync(BitrixPortal portal, int bitrixDepartmentId)
        {
            try
            {
                var payload = new
                {
                    ID = bitrixDepartmentId
                };

                return await SendRequestAsync(portal.WebhookUrl, "department.delete", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка удаления подразделения {bitrixDepartmentId} с портала {portal.Name}");
                return new BitrixApiResponse 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }

        public async Task<BitrixApiResponse?> SyncUserAsync(BitrixPortal portal, Employee employee, string action)
        {
            try
            {
                // Используем маппинг полей вместо жестко заданных имен
                var fields = await _fieldMappingService.MapToBitrixAsync(portal.Id, employee);
                
                if (!fields.Any())
                {
                    _logger.LogWarning($"Нет настроек маппинга для сотрудника {employee.Id}, используем значения по умолчанию");
                    fields = new Dictionary<string, object>
                    {
                        ["EMAIL"] = employee.Email,
                        ["LOGIN"] = employee.Email,
                        ["NAME"] = employee.FirstName,
                        ["LAST_NAME"] = employee.LastName,
                        ["SECOND_NAME"] = employee.MiddleName ?? "",
                        ["POSITION"] = employee.Position?.Name ?? "",
                        ["DEPARTMENT"] = employee.PortalId > 0 ? employee.PortalId : null,
                        ["UF_EMPLOYEE_RANK"] = employee.Rank?.Name ?? "",
                        ["UF_EMPLOYEE_QUALIFICATION"] = employee.Qualification?.Name ?? "",
                        ["UF_EMPLOYEE_STATUS"] = ((int)employee.Status).ToString(),
                        ["WORK_PHONE"] = employee.Phone ?? "",
                        ["PERSONAL_PHOTO"] = employee.Photo ?? ""
                    };
                }

                var payload = new
                {
                    fields,
                    params_action = action
                };

                return await SendRequestAsync(portal.WebhookUrl, "user.item.save", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка синхронизации пользователя {employee.GetShortName()} с порталом {portal.Name}");
                return new BitrixApiResponse 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }

        public async Task<BitrixApiResponse?> DeleteUserAsync(BitrixPortal portal, int bitrixUserId)
        {
            try
            {
                var payload = new
                {
                    ID = bitrixUserId
                };

                return await SendRequestAsync(portal.WebhookUrl, "user.delete", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка удаления пользователя {bitrixUserId} с портала {portal.Name}");
                return new BitrixApiResponse 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }

        public async Task<BitrixApiResponse?> SyncTaskAsync(BitrixPortal portal, EmployeeTask task, string action)
        {
            try
            {
                // Используем маппинг полей вместо жестко заданных имен
                var fields = await _fieldMappingService.MapToBitrixAsync(portal.Id, task);
                
                if (!fields.Any())
                {
                    _logger.LogWarning($"Нет настроек маппинга для задачи {task.Id}, используем значения по умолчанию");
                    var status = TranslateStatusToBitrix(task.Status);
                    fields = new Dictionary<string, object>
                    {
                        ["TITLE"] = task.Title,
                        ["DESCRIPTION"] = task.Description ?? "",
                        ["DEADLINE"] = task.DeadLine?.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                        ["STATUS"] = status,
                        ["RESPONSIBLE_ID"] = task.Employee?.BitrixUserId ?? 0,
                        ["CREATED_BY"] = task.Author?.BitrixUserId ?? 0,
                        ["UF_TASK_COST"] = task.Cost.ToString("F2"),
                        ["UF_TASK_PRIORITY"] = task.Priority?.ToString() ?? "1"
                    };
                }

                var payload = new
                {
                    fields,
                    params_action = action
                };

                return await SendRequestAsync(portal.WebhookUrl, "tasks.task.update", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка синхронизации задачи {task.Title} с порталом {portal.Name}");
                return new BitrixApiResponse 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }

        public async Task<BitrixApiResponse?> DeleteTaskAsync(BitrixPortal portal, int bitrixTaskId)
        {
            try
            {
                var payload = new
                {
                    taskId = bitrixTaskId
                };

                return await SendRequestAsync(portal.WebhookUrl, "tasks.task.delete", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка удаления задачи {bitrixTaskId} с портала {portal.Name}");
                return new BitrixApiResponse 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }

        private async Task<BitrixApiResponse> SendRequestAsync(string webhookUrl, string method, object payload)
        {
            var url = $"{webhookUrl}{method}";
            
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new BitrixApiResponse
                {
                    Success = false,
                    Error = $"HTTP {response.StatusCode}: {responseString}",
                    RawResponse = responseString
                };
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseString);
            
            // Проверяем наличие ошибки в ответе Bitrix
            if (result.TryGetProperty("error", out var errorProp))
            {
                return new BitrixApiResponse
                {
                    Success = false,
                    Error = errorProp.GetString(),
                    RawResponse = responseString
                };
            }

            // Получаем ID созданного/обновленного объекта
            int? externalId = null;
            if (result.TryGetProperty("result", out var resultProp))
            {
                if (resultProp.TryGetProperty("id", out var idProp))
                {
                    externalId = idProp.GetInt32();
                }
                else if (resultProp.TryGetProperty("TASK_ID", out var taskIdProp))
                {
                    externalId = taskIdProp.GetInt32();
                }
            }

            return new BitrixApiResponse
            {
                Success = true,
                ExternalId = externalId,
                RawResponse = responseString
            };
        }

        private string TranslateStatusToBitrix(Models.TaskStatus status)
        {
            return status switch
            {
                Models.TaskStatus.New => "1",
                Models.TaskStatus.InProgress => "2",
                Models.TaskStatus.Finished => "5",
                _ => "1"
            };
        }
    }
}
