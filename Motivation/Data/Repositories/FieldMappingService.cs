using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Motivation.Data;
using Motivation.Models;

namespace Motivation.Data.Repositories
{
    /// <summary>
    /// Сервис для управления маппингом полей между локальной системой и Bitrix24
    /// </summary>
    public interface IFieldMappingService
    {
        /// <summary>
        /// Получить все активные маппинги для портала и сущности
        /// </summary>
        Task<List<FieldMapping>> GetMappingsAsync(int portalId, string entityType);

        /// <summary>
        /// Преобразовать локальный объект в словарь полей Bitrix24 согласно настройкам маппинга
        /// </summary>
        Task<Dictionary<string, object>> MapToBitrixAsync<T>(int portalId, T entity) where T : class;

        /// <summary>
        /// Преобразовать данные из Bitrix24 в локальную модель согласно настройкам маппинга
        /// </summary>
        Task<T?> MapFromBitrixAsync<T>(int portalId, Dictionary<string, object> bitrixData) where T : class, new();

        /// <summary>
        /// Сохранить настройки маппинга
        /// </summary>
        Task SaveMappingsAsync(List<FieldMapping> mappings);

        /// <summary>
        /// Получить маппинг по конкретному локальному полю
        /// </summary>
        Task<FieldMapping?> GetMappingAsync(int portalId, string entityType, string localFieldName);

        /// <summary>
        /// Получить маппинг по ID
        /// </summary>
        Task<FieldMapping?> GetByIdAsync(int id);

        /// <summary>
        /// Удалить маппинг
        /// </summary>
        Task DeleteAsync(int id);
    }

    public class FieldMappingService : IFieldMappingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FieldMappingService> _logger;

        public FieldMappingService(ApplicationDbContext context, ILogger<FieldMappingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<FieldMapping>> GetMappingsAsync(int portalId, string entityType)
        {
            return await _context.FieldMappings
                .Where(fm => fm.PortalId == portalId 
                          && fm.EntityType == entityType 
                          && fm.IsActive)
                .OrderBy(fm => fm.LocalFieldName)
                .ToListAsync();
        }

        public async Task<FieldMapping?> GetMappingAsync(int portalId, string entityType, string localFieldName)
        {
            return await _context.FieldMappings
                .FirstOrDefaultAsync(fm => fm.PortalId == portalId
                                          && fm.EntityType == entityType
                                          && fm.LocalFieldName == localFieldName
                                          && fm.IsActive);
        }

        public async Task<FieldMapping?> GetByIdAsync(int id)
        {
            return await _context.FieldMappings
                .FirstOrDefaultAsync(fm => fm.Id == id);
        }

        public async Task DeleteAsync(int id)
        {
            var mapping = await _context.FieldMappings
                .FirstOrDefaultAsync(fm => fm.Id == id);
            
            if (mapping != null)
            {
                _context.FieldMappings.Remove(mapping);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Dictionary<string, object>> MapToBitrixAsync<T>(int portalId, T entity) where T : class
        {
            var result = new Dictionary<string, object>();
            var entityType = typeof(T).Name;
            
            var mappings = await GetMappingsAsync(portalId, entityType);
            
            if (!mappings.Any())
            {
                _logger.LogWarning($"Не найдено настроек маппинга для сущности {entityType} портала {portalId}");
                return result;
            }

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var mapping in mappings)
            {
                try
                {
                    var property = properties.FirstOrDefault(p => p.Name == mapping.LocalFieldName);
                    if (property == null)
                    {
                        _logger.LogWarning($"Свойство {mapping.LocalFieldName} не найдено в сущности {entityType}");
                        continue;
                    }

                    var value = property.GetValue(entity);
                    if (value != null)
                    {
                        var bitrixValue = ApplyMappingType(value, mapping.MappingType);
                        result[mapping.BitrixCode] = bitrixValue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка маппинга поля {mapping.LocalFieldName} -> {mapping.BitrixCode}");
                }
            }

            return result;
        }

        public async Task<T?> MapFromBitrixAsync<T>(int portalId, Dictionary<string, object> bitrixData) where T : class, new()
        {
            var entityType = typeof(T).Name;
            var mappings = await GetMappingsAsync(portalId, entityType);

            if (!mappings.Any())
            {
                _logger.LogWarning($"Не найдено настроек маппинга для сущности {entityType} портала {portalId}");
                return null;
            }

            var entity = new T();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var mapping in mappings)
            {
                try
                {
                    if (!bitrixData.TryGetValue(mapping.BitrixCode, out var bitrixValue) || bitrixValue == null)
                    {
                        continue;
                    }

                    var property = properties.FirstOrDefault(p => p.Name == mapping.LocalFieldName);
                    if (property == null || !property.CanWrite)
                    {
                        continue;
                    }

                    var localValue = ConvertToType(bitrixValue, property.PropertyType);
                    if (localValue != null)
                    {
                        property.SetValue(entity, localValue);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка обратного маппинга поля {mapping.BitrixCode} -> {mapping.LocalFieldName}");
                }
            }

            return entity;
        }

        public async Task SaveMappingsAsync(List<FieldMapping> mappings)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var mapping in mappings)
                {
                    var existing = await _context.FieldMappings
                        .FirstOrDefaultAsync(fm => fm.Id == mapping.Id);

                    if (existing != null)
                    {
                        existing.BitrixCode = mapping.BitrixCode;
                        existing.Description = mapping.Description;
                        existing.IsActive = mapping.IsActive;
                        existing.MappingType = mapping.MappingType;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (mapping.Id == 0)
                    {
                        await _context.FieldMappings.AddAsync(mapping);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка сохранения настроек маппинга");
                throw;
            }
        }

        private object? ApplyMappingType(object value, string mappingType)
        {
            return mappingType switch
            {
                "Direct" => value,
                "ConvertToDecimal" => Convert.ToDecimal(value),
                "ToString" => value?.ToString(),
                _ => value
            };
        }

        private object? ConvertToType(object bitrixValue, Type targetType)
        {
            try
            {
                if (bitrixValue == null)
                {
                    return null;
                }

                var nullableTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                if (bitrixValue.GetType() == nullableTargetType)
                {
                    return bitrixValue;
                }

                if (nullableTargetType == typeof(string))
                {
                    return bitrixValue.ToString();
                }

                if (nullableTargetType == typeof(int) || nullableTargetType == typeof(int?))
                {
                    if (int.TryParse(bitrixValue.ToString(), out var intVal))
                    {
                        return intVal;
                    }
                    return null;
                }

                if (nullableTargetType == typeof(decimal) || nullableTargetType == typeof(decimal?))
                {
                    if (decimal.TryParse(bitrixValue.ToString(), out var decVal))
                    {
                        return decVal;
                    }
                    return null;
                }

                if (nullableTargetType == typeof(DateTime) || nullableTargetType == typeof(DateTime?))
                {
                    if (DateTime.TryParse(bitrixValue.ToString(), out var dateVal))
                    {
                        return dateVal;
                    }
                    return null;
                }

                return Convert.ChangeType(bitrixValue, nullableTargetType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка конвертации типа {bitrixValue.GetType()} в {targetType}");
                return null;
            }
        }
    }
}
