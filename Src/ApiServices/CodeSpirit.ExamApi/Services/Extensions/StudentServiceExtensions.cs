using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.Student;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Dtos.Common;
using CodeSpirit.Shared.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Services.Extensions
{
    /// <summary>
    /// StudentService的增强批量导入扩展
    /// </summary>
    public static class StudentServiceExtensions
    {
        /// <summary>
        /// 为StudentService添加增强批量导入功能
        /// </summary>
        /// <param name="studentService">学生服务</param>
        /// <param name="helper">批量导入助手</param>
        /// <param name="mapper">映射器</param>
        /// <returns>增强的批量导入结果</returns>
        public static async Task<BatchImportResultDto> EnhancedBatchImportAsync(
            this IStudentService studentService,
            EnhancedBatchImportHelper<StudentBatchImportDto> helper,
            IMapper mapper,
            IEnumerable<StudentBatchImportDto> importData)
        {
            return await helper.EnhancedBatchImportAsync(
                importData,
                async (dto, index) =>
                {
                    try
                    {
                        // 检查身份证号是否已存在
                        var existingStudent = await studentService.GetStudentByIdNoAsync(dto.IdNo);
                        if (existingStudent != null)
                        {
                            return $"身份证号 {dto.IdNo} 已存在";
                        }

                        // 映射并创建学生
                        var createDto = mapper.Map<CreateStudentDto>(dto);
                        await studentService.CreateAsync(createDto);
                        
                        return null; // 成功
                    }
                    catch (Exception ex)
                    {
                        return ex.Message; // 返回错误消息
                    }
                },
                async (dto, index) =>
                {
                    // 自定义验证逻辑
                    var errors = new List<ValidationError>();
                    
                    // 验证身份证号格式
                    if (!IsValidIdNumber(dto.IdNo))
                    {
                        errors.Add(new ValidationError
                        {
                            Index = index,
                            ErrorMessage = "身份证号格式不正确",
                            ErrorFields = new List<string> { nameof(dto.IdNo) }
                        });
                    }
                    
                    // 验证手机号格式
                    if (!string.IsNullOrEmpty(dto.PhoneNumber) && !IsValidPhoneNumber(dto.PhoneNumber))
                    {
                        errors.Add(new ValidationError
                        {
                            Index = index,
                            ErrorMessage = "手机号格式不正确",
                            ErrorFields = new List<string> { nameof(dto.PhoneNumber) }
                        });
                    }
                    
                    return errors;
                }
            );
        }

        /// <summary>
        /// 获取导入结果
        /// </summary>
        /// <param name="studentService">学生服务</param>
        /// <param name="helper">批量导入助手</param>
        /// <param name="importId">导入ID</param>
        /// <returns>导入结果</returns>
        public static async Task<BatchImportResultDto?> GetImportResultAsync(
            this IStudentService studentService,
            EnhancedBatchImportHelper<StudentBatchImportDto> helper,
            string importId)
        {
            return await helper.GetImportResultAsync(importId);
        }

        /// <summary>
        /// 导出失败记录
        /// </summary>
        /// <param name="studentService">学生服务</param>
        /// <param name="helper">批量导入助手</param>
        /// <param name="failedRecords">失败记录</param>
        /// <returns>Excel文件字节数组</returns>
        public static async Task<byte[]> ExportFailedRecordsAsync(
            this IStudentService studentService,
            EnhancedBatchImportHelper<StudentBatchImportDto> helper,
            List<ImportFailedRecord> failedRecords)
        {
            return await helper.ExportFailedRecordsAsync(failedRecords);
        }

        #region Private Helper Methods

        /// <summary>
        /// 验证身份证号格式
        /// </summary>
        /// <param name="idNumber">身份证号</param>
        /// <returns>是否有效</returns>
        private static bool IsValidIdNumber(string idNumber)
        {
            if (string.IsNullOrWhiteSpace(idNumber))
                return false;
                
            // 简单的身份证号验证（18位数字，最后一位可能是X）
            return idNumber.Length == 18 && 
                   idNumber.Take(17).All(char.IsDigit) && 
                   (char.IsDigit(idNumber[17]) || idNumber[17] == 'X');
        }

        /// <summary>
        /// 验证手机号格式
        /// </summary>
        /// <param name="phoneNumber">手机号</param>
        /// <returns>是否有效</returns>
        private static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return true; // 允许为空
                
            // 简单的手机号验证（11位数字，以1开头）
            return phoneNumber.Length == 11 && 
                   phoneNumber.All(char.IsDigit) && 
                   phoneNumber.StartsWith("1");
        }

        #endregion
    }
}
