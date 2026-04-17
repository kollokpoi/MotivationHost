using Microsoft.AspNetCore.Mvc;
using Motivation.Data.Repositories;
using Motivation.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Motivation.Controllers
{
    /// <summary>
    /// Контроллер для обработки входящих вебхуков от Bitrix24
    /// </summary>
    [ApiController]
    [Route("api/bitrix/webhook")]
    public class BitrixWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BitrixWebhookController> _logger;

        public BitrixWebhookController(
            ApplicationDbContext context,
            ILogger<BitrixWebhookController> logger
        )
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Обработчик входящих вебхуков от Bitrix24
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> HandleWebhook([FromBody] JsonElement payload)
        {
            try
            {
                // Получаем заголовок с подписью
                if (!Request.Headers.TryGetValue("X-Bitrix-Signature", out var signatureHeader))
                {
                    _logger.LogWarning("Webhook received without signature");
                }

                // Извлекаем данные из webhook
                var eventType = payload.TryGetProperty("event", out var eventElement)
                    ? eventElement.GetString()
                    : null;

                var data = payload.TryGetProperty("data", out var dataElement)
                    ? dataElement
                    : default;

                _logger.LogInformation($"Received Bitrix webhook: {eventType}");

                // Обрабатываем различные типы событий
                switch (eventType)
                {
                    case "onTaskAdd":
                        await HandleTaskEvent(data, TaskEventType.Add);
                        break;
                    case "onTaskUpdate":
                        await HandleTaskEvent(data, TaskEventType.Update);
                        break;
                    case "onTaskDelete":
                        await HandleTaskEvent(data, TaskEventType.Delete);
                        break;
                    case "onDepartmentAdd":
                    case "onDepartmentUpdate":
                    case "onDepartmentDelete":
                        await HandleDepartmentEvent(eventType, data);
                        break;
                    case "onUserAdd":
                    case "onUserUpdate":
                    case "onUserDelete":
                        await HandleUserEvent(eventType, data);
                        break;
                    default:
                        _logger.LogInformation($"Unhandled webhook event type: {eventType}");
                        break;
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Bitrix webhook");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Проверка подписи вебхука
        /// </summary>
        private bool VerifyWebhookSignature(string payload, string signature, string secret)
        {
            if (string.IsNullOrEmpty(secret))
            {
                // Если секрет не установлен, пропускаем проверку
                return true;
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToBase64String(hash);

            return computedSignature == signature;
        }

        private async Task HandleTaskEvent(JsonElement data, TaskEventType eventType)
        {
            if (data.ValueKind == JsonElement.Undefined)
                return;

            var taskId = data.TryGetProperty("ID", out var idElement)
                ? idElement.GetInt32()
                : 0;

            if (taskId == 0)
                return;

            _logger.LogInformation($"Processing task {eventType}: ID={taskId}");

            // Получаем активный портал
            var portal = await _context.BitrixPortals.FirstOrDefaultAsync(p => p.IsActive);
            if (portal == null)
            {
                _logger.LogWarning("No active portal found for task sync");
                return;
            }

            switch (eventType)
            {
                case TaskEventType.Add:
                case TaskEventType.Update:
                    await SyncTaskFromBitrix(taskId, data, portal);
                    break;
                case TaskEventType.Delete:
                    await DeleteTaskFromLocal(taskId, portal);
                    break;
            }
        }

        private async Task SyncTaskFromBitrix(int bitrixTaskId, JsonElement data, BitrixPortal portal)
        {
            // Пытаемся найти существующую задачу по BitrixUserId
            var localTask = await _context.EmployeeTasks
                .FirstOrDefaultAsync(t => t.PortalId == portal.Id && t.Title.Contains(bitrixTaskId.ToString()));

            if (localTask == null)
            {
                // Создаем новую задачу
                // TODO: Найти ответственного сотрудника и создать задачу
                _logger.LogInformation($"Creating new task from Bitrix: {bitrixTaskId}");
            }
            else
            {
                // Обновляем существующую задачу
                _logger.LogInformation($"Updating task from Bitrix: {bitrixTaskId}");
            }

            await _context.SaveChangesAsync();
        }

        private async Task DeleteTaskFromLocal(int bitrixTaskId, BitrixPortal portal)
        {
            var task = await _context.EmployeeTasks
                .FirstOrDefaultAsync(t => t.PortalId == portal.Id && t.Title.Contains(bitrixTaskId.ToString()));

            if (task != null)
            {
                _context.EmployeeTasks.Remove(task);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted task from local DB: {bitrixTaskId}");
            }
        }

        private async Task HandleDepartmentEvent(string eventType, JsonElement data)
        {
            _logger.LogInformation($"Processing department event: {eventType}");
            // TODO: Реализовать синхронизацию подразделений
            await Task.CompletedTask;
        }

        private async Task HandleUserEvent(string eventType, JsonElement data)
        {
            _logger.LogInformation($"Processing user event: {eventType}");
            // TODO: Реализовать синхронизацию пользователей
            await Task.CompletedTask;
        }
    }

    public enum TaskEventType
    {
        Add,
        Update,
        Delete
    }
}
