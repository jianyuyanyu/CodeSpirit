using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.Extensions;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Extensions.Extensions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CodeSpirit.ExamApi.Services.Implementations
{
    /// <summary>
    /// 试卷服务实现
    /// </summary>
    public class ExamPaperService : BaseCRUDService<ExamPaper, ExamPaperDto, long, CreateExamPaperDto, UpdateExamPaperDto>, IExamPaperService
    {
        private readonly IRepository<ExamPaper> _examPaperRepository;
        private readonly IRepository<ExamPaperQuestion> _examPaperQuestionRepository;
        private readonly IRepository<Question> _questionRepository;
        private readonly IRepository<QuestionVersion> _questionVersionRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ExamPaperService> _logger;
        private readonly IScoreConversionService _scoreConversionService;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ExamPaperService(
            IRepository<ExamPaper> examPaperRepository,
            IRepository<ExamPaperQuestion> examPaperQuestionRepository,
            IRepository<Question> questionRepository,
            IRepository<QuestionVersion> questionVersionRepository,
            IMapper mapper,
            ILogger<ExamPaperService> logger,
            IScoreConversionService scoreConversionService)
            : base(examPaperRepository, mapper)
        {
            _examPaperRepository = examPaperRepository;
            _examPaperQuestionRepository = examPaperQuestionRepository;
            _questionRepository = questionRepository;
            _questionVersionRepository = questionVersionRepository;
            _mapper = mapper;
            _logger = logger;
            _scoreConversionService = scoreConversionService;
        }

        /// <summary>
        /// 获取试卷查询表达式
        /// </summary>
        private ExpressionStarter<ExamPaper> GetExamPaperQueryPredicate(ExamPaperQueryDto queryDto)
        {
            var predicate = PredicateBuilder.New<ExamPaper>(true);

            if (!string.IsNullOrEmpty(queryDto.Keywords))
            {
                predicate = predicate.And(x => x.Name.Contains(queryDto.Keywords) ||
                                          (x.Description != null && x.Description.Contains(queryDto.Keywords)));
            }

            if (queryDto.Type.HasValue)
            {
                predicate = predicate.And(x => x.Type == queryDto.Type.Value);
            }

            if (queryDto.Status.HasValue)
            {
                predicate = predicate.And(x => x.Status == queryDto.Status.Value);
            }

            if (queryDto.MinDifficultyLevel.HasValue)
            {
                predicate = predicate.And(x => x.DifficultyLevel >= queryDto.MinDifficultyLevel.Value);
            }

            if (queryDto.MaxDifficultyLevel.HasValue)
            {
                predicate = predicate.And(x => x.DifficultyLevel <= queryDto.MaxDifficultyLevel.Value);
            }

            return predicate;
        }

        /// <summary>
        /// 获取分页列表
        /// </summary>
        public async Task<PageList<ExamPaperDto>> GetExamPapersAsync(ExamPaperQueryDto queryDto)
        {
            var predicate = GetExamPaperQueryPredicate(queryDto);
            return await base.GetPagedListAsync(queryDto, predicate);
        }

        /// <summary>
        /// 获取带题目列表的试卷
        /// </summary>
        public override async Task<ExamPaperDto> GetAsync(long id)
        {
            var examPaper = await _examPaperRepository
                .Find(p => p.Id == id)
                .Include(p => p.ExamPaperQuestions)
                    .ThenInclude(q => q.Question)
                .Include(p => p.ExamPaperQuestions)
                    .ThenInclude(q => q.QuestionVersion)
                .FirstOrDefaultAsync();

            if (examPaper == null)
            {
                return null;
            }

            var examPaperDto = _mapper.Map<ExamPaperDto>(examPaper);
            examPaperDto.Questions = _mapper.Map<List<ExamPaperQuestionDto>>(
                examPaper.ExamPaperQuestions.OrderBy(q => q.OrderNumber).ToList());

            return examPaperDto;
        }

        /// <summary>
        /// 创建试卷
        /// </summary>
        public override async Task<ExamPaperDto> CreateAsync(CreateExamPaperDto createDto)
        {
            var examPaper = _mapper.Map<ExamPaper>(createDto);
            examPaper.Status = ExamPaperStatus.Draft;
            examPaper.Version = 1;
            examPaper.UsageCount = 0;
            examPaper.AverageScore = 0;
            examPaper.PassRate = 0;

            var lastVersionQuestions = await _questionVersionRepository.CreateQuery().Include(s => s.Question)
                .Where(s => createDto.QuestionIds.Select(i => long.Parse(i)).ToList().Contains(s.QuestionId))
                .GroupBy(s => s.QuestionId)
                .Select(g => g.OrderByDescending(s => s.Version).First())
                .ToListAsync();

            var questions = lastVersionQuestions.Select(s => s.Question).ToList();

            // 计算难度系数
            if (questions != null && questions.Count != 0)
            {
                // 根据题目难度计算试卷难度系数
                examPaper.DifficultyLevel = CalculateDifficultyLevel(questions);
            }

            await _examPaperRepository.AddAsync(examPaper);

            // 添加试卷题目
            if (lastVersionQuestions != null && lastVersionQuestions.Count != 0)
            {
                var examPaperQuestions = new List<ExamPaperQuestion>();
                var order = 1;
                foreach (var questionDto in lastVersionQuestions)
                {
                    var examPaperQuestion = _mapper.Map<ExamPaperQuestion>(questionDto);
                    examPaperQuestion.ExamPaperId = examPaper.Id;
                    examPaperQuestion.OrderNumber = order++;
                    examPaperQuestions.Add(examPaperQuestion);
                }

                await _examPaperQuestionRepository.AddRangeAsync(examPaperQuestions);
            }

            return await GetAsync(examPaper.Id);
        }

        /// <summary>
        /// 更新试卷
        /// </summary>
        public override async Task UpdateAsync(long id, UpdateExamPaperDto updateDto)
        {
            var examPaper = await _examPaperRepository
                .Find(p => p.Id == id)
                .Include(p => p.ExamPaperQuestions)
                .FirstOrDefaultAsync();

            if (examPaper == null)
            {
                throw new AppServiceException(404, "试卷不存在");
            }

            // 检查试卷状态，只有草稿状态的试卷可以更新
            if (examPaper.Status != ExamPaperStatus.Draft)
            {
                throw new AppServiceException(400, "只有草稿状态的试卷可以更新");
            }

            // 更新基本信息
            _mapper.Map(updateDto, examPaper);

            await _examPaperRepository.UpdateAsync(examPaper);
        }

        /// <summary>
        /// 删除试卷
        /// </summary>
        public override async Task DeleteAsync(long id)
        {
            var examPaper = await _examPaperRepository
                .Find(p => p.Id == id)
                .Include(p => p.ExamPaperQuestions)
                .FirstOrDefaultAsync();

            if (examPaper == null)
            {
                return;
            }

            // 检查试卷状态，已发布的试卷不能删除
            if (examPaper.Status == ExamPaperStatus.Published)
            {
                throw new AppServiceException(400, "已发布的试卷不能删除");
            }

            // 删除试卷相关题目
            await _examPaperRepository.ExecuteInTransactionAsync(async () =>
            {
                foreach (var question in examPaper.ExamPaperQuestions)
                {
                    await _examPaperQuestionRepository.DeleteAsync(question);
                }

                // 删除试卷
                await _examPaperRepository.DeleteAsync(examPaper);
            });
        }

        /// <summary>
        /// 发布试卷
        /// </summary>
        public async Task PublishExamPaperAsync(long id)
        {
            var examPaper = await _examPaperRepository
                .Find(p => p.Id == id)
                .Include(p => p.ExamPaperQuestions)
                .FirstOrDefaultAsync();

            if (examPaper == null)
            {
                throw new AppServiceException(404, "试卷不存在");
            }

            // 检查试卷状态
            if (examPaper.Status == ExamPaperStatus.Published)
            {
                throw new AppServiceException(400, "试卷已经是发布状态");
            }

            // 检查是否有题目
            if (!examPaper.ExamPaperQuestions.Any())
            {
                throw new AppServiceException(400, "试卷没有题目，不能发布");
            }
            
            // 检查是否已预览
            if (!examPaper.IsPreviewChecked)
            {
                throw new AppServiceException(400, "试卷发布前必须先完成预览操作");
            }

            // 发布试卷
            examPaper.Status = ExamPaperStatus.Published;
            await _examPaperRepository.UpdateAsync(examPaper);
        }

        /// <summary>
        /// 取消发布试卷
        /// </summary>
        public async Task UnpublishExamPaperAsync(long id)
        {
            var examPaper = await _examPaperRepository.GetByIdAsync(id);
            if (examPaper == null)
            {
                throw new AppServiceException(404, "试卷不存在");
            }

            // 检查试卷状态
            if (examPaper.Status != ExamPaperStatus.Published)
            {
                throw new AppServiceException(400, "试卷不是发布状态");
            }

            // 取消发布
            examPaper.Status = ExamPaperStatus.Draft;
            await _examPaperRepository.UpdateAsync(examPaper);
        }

        /// <summary>
        /// 生成随机试卷
        /// </summary>
        public async Task<ExamPaperDto> GenerateRandomExamPaperAsync(GenerateRandomExamPaperDto createDto)
        {
            // 验证参数
            await ValidateRandomExamPaperRules(createDto);

            ExamPaper? examPaper = null;
            // 使用事务包装整个过程
            await _examPaperRepository.ExecuteInTransactionAsync(async () =>
            {
                // 创建试卷基本信息
                examPaper = new ExamPaper
                {
                    Name = createDto.Name,
                    Description = createDto.Description,
                    Type = ExamPaperType.Random,
                    TotalScore = createDto.TotalScore,
                    PassScore = createDto.PassScore,
                    Duration = createDto.Duration,
                    Status = ExamPaperStatus.Draft,
                    Version = 1,
                    UsageCount = 0,
                    AverageScore = 0,
                    PassRate = 0
                };

                // 处理成绩换算配置
                if (createDto.EnableScoreConversion)
                {
                    // 验证换算配置
                    var validationResult = _scoreConversionService.ValidateConversionConfiguration(
                        createDto.TotalScore,
                        createDto.ConversionTargetFullScore,
                        createDto.ConversionTargetPassScore,
                        createDto.ConversionDecimalPlaces);

                    if (!validationResult.IsValid)
                    {
                        throw new AppServiceException(400, $"成绩换算配置无效：{validationResult.ErrorMessage}");
                    }

                    // 设置换算配置
                    examPaper.EnableScoreConversion = true;
                    examPaper.OriginalPassScore = createDto.PassScore; // 保存原始及格分
                    examPaper.ConversionTargetFullScore = createDto.ConversionTargetFullScore;
                    examPaper.ConversionDecimalPlaces = createDto.ConversionDecimalPlaces;
                    
                    // 计算换算比例
                    examPaper.ConversionRatio = _scoreConversionService.CalculateConversionRatio(
                        createDto.TotalScore, createDto.ConversionTargetFullScore);

                    // 更新PassScore为换算后的目标及格分
                    examPaper.PassScore = createDto.ConversionTargetPassScore;

                    _logger.LogInformation(
                        "试卷 {ExamPaperName} 启用成绩换算：{OriginalFullScore} → {TargetFullScore}，换算比例：{Ratio}",
                        examPaper.Name, createDto.TotalScore, createDto.ConversionTargetFullScore, examPaper.ConversionRatio);
                }

                // 保存随机规则
                var randomRules = new
                {
                    QuestionTypeRules = createDto.QuestionTypeRules,
                    DifficultyRules = createDto.DifficultyRules,
                    CategoryIds = createDto.CategoryIds,
                    Tags = createDto.Tags
                };
                examPaper.RandomRules = JsonSerializer.Serialize(randomRules);

                await _examPaperRepository.AddAsync(examPaper);

                // 根据规则选择题目
                var examPaperQuestions = await GenerateQuestionsBasedOnRules(examPaper.Id, createDto);

                // 添加试卷题目并计算难度
                if (examPaperQuestions.Any())
                {
                    await _examPaperQuestionRepository.AddRangeAsync(examPaperQuestions);
                    await UpdateExamPaperDifficulty(examPaper, examPaperQuestions);
                }
            });
            return await GetAsync(examPaper.Id);
        }

        public async Task<IEnumerable<ExamPaperDto>> GetAllExamPapersByStatusAsync(ExamPaperStatus examPaperStatus = ExamPaperStatus.Published)
        {
            var entities = await Repository.CreateQuery()
                .Where(p => p.Status == examPaperStatus)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return Mapper.Map<IEnumerable<ExamPaperDto>>(entities);
        }

        private async Task ValidateRandomExamPaperRules(GenerateRandomExamPaperDto createDto)
        {
            // 总分与各题型分数之和必须相等
            if (createDto.TotalScore!= createDto.QuestionTypeRules.Sum(r => r.ScorePerQuestion * r.Count))
            {
                throw new AppServiceException(400, "总分与各题型分数之和必须相等");
            }

            // 预先检查题库是否有足够的题目
            foreach (var typeRule in createDto.QuestionTypeRules)
            {
                var query = _questionRepository.Find(q => q.Type == typeRule.QuestionType);
                
                // 如果指定了分类ID，则添加分类条件
                if (createDto.CategoryIds != null && createDto.CategoryIds.Any())
                {
                    query = query.Where(q => createDto.CategoryIds.Contains(q.CategoryId));
                }
                
                // 如果指定了标签，则添加标签条件
                if (createDto.Tags != null && createDto.Tags.Any())
                {
                    query = query.Where(q => q.Tags != null && createDto.Tags.All(tag => q.Tags.Contains(tag)));
                }
                
                var availableCount = await query.CountAsync();

                if (availableCount < typeRule.Count)
                {
                    var filterMessage = new List<string>();
                    
                    if (createDto.CategoryIds != null && createDto.CategoryIds.Any())
                    {
                        filterMessage.Add("指定的分类");
                    }
                    
                    if (createDto.Tags != null && createDto.Tags.Any())
                    {
                        filterMessage.Add("指定的标签");
                    }
                    
                    var filterText = filterMessage.Any() ? $"在{string.Join("和", filterMessage)}中，" : "";
                    
                    throw new AppServiceException(400,
                        $"{filterText}题库中类型为{typeRule.QuestionType}的题目不足，需要{typeRule.Count}题，实际只有{availableCount}题");
                }
            }
        }

        private async Task<List<ExamPaperQuestion>> GenerateQuestionsBasedOnRules(long examPaperId, GenerateRandomExamPaperDto createDto)
        {
            var examPaperQuestions = new List<ExamPaperQuestion>();
            var orderNumber = 1;

            foreach (var typeRule in createDto.QuestionTypeRules)
            {
                // 按难度比例选择题目，并考虑标签筛选
                var questions = await SelectQuestionsByDifficulty(typeRule, createDto.DifficultyRules, createDto.CategoryIds, createDto.Tags);

                foreach (var question in questions)
                {
                    var latestVersion = await GetLatestQuestionVersion(question.Id);
                    if (latestVersion != null)
                    {
                        examPaperQuestions.Add(new ExamPaperQuestion
                        {
                            ExamPaperId = examPaperId,
                            QuestionId = question.Id,
                            QuestionVersionId = latestVersion.Id,
                            OrderNumber = orderNumber++,
                            Score = typeRule.ScorePerQuestion,
                            IsRequired = true
                        });
                    }
                }
            }

            return examPaperQuestions;
        }

        private async Task<List<Question>> SelectQuestionsByDifficulty(
            QuestionTypeRule typeRule,
            List<DifficultyRule> difficultyRules,
            List<long> categoryIds = null,
            List<string> tags = null)
        {
            var questions = new List<Question>();

            // 基础查询条件：题目类型
            var baseQuery = _questionRepository.Find(q => q.Type == typeRule.QuestionType);
            
            // 如果指定了分类ID，则添加分类条件
            if (categoryIds != null && categoryIds.Any())
            {
                baseQuery = baseQuery.Where(q => categoryIds.Contains(q.CategoryId));
            }
            
            // 如果指定了标签，则添加标签条件
            if (tags != null && tags.Any())
            {
                baseQuery = baseQuery.Where(q => q.Tags != null && tags.All(tag => q.Tags.Contains(tag)));
            }

            // 如果没有难度规则，则随机选择指定数量的题目
            if (difficultyRules == null || !difficultyRules.Any())
            {
                var randomQuestions = await baseQuery
                    .OrderBy(q => Guid.NewGuid())
                    .Take(typeRule.Count)
                    .ToListAsync();

                questions.AddRange(randomQuestions);
                return questions;
            }

            // 如果有难度规则，按难度比例选择题目
            foreach (var difficultyRule in difficultyRules.Where(r => r.Percentage > 0))
            {
                var count = (int)Math.Ceiling(typeRule.Count * difficultyRule.Percentage / 100.0);
                var difficultyQuestions = await baseQuery
                    .Where(q => q.Difficulty == difficultyRule.Difficulty)
                    .OrderBy(q => Guid.NewGuid())
                    .Take(count)
                    .ToListAsync();

                questions.AddRange(difficultyQuestions);
            }

            // 如果按难度规则选择的题目数量不足，则补充随机题目
            if (questions.Count < typeRule.Count)
            {
                var remainingCount = typeRule.Count - questions.Count;
                var existingQuestionIds = questions.Select(q => q.Id).ToList();

                var additionalQuestions = await baseQuery
                    .Where(q => !existingQuestionIds.Contains(q.Id))
                    .OrderBy(q => Guid.NewGuid())
                    .Take(remainingCount)
                    .ToListAsync();

                questions.AddRange(additionalQuestions);
            }

            return questions;
        }

        /// <summary>
        /// 复制试卷
        /// </summary>
        public async Task<ExamPaperDto> CopyExamPaperAsync(long id)
        {
            var examPaper = await _examPaperRepository
                .Find(p => p.Id == id)
                .Include(p => p.ExamPaperQuestions)
                .FirstOrDefaultAsync();

            if (examPaper == null)
            {
                throw new AppServiceException(404, "试卷不存在");
            }

            // 创建新试卷
            var newExamPaper = new ExamPaper
            {
                Name = $"{examPaper.Name}_副本",
                Description = examPaper.Description,
                Type = examPaper.Type,
                TotalScore = examPaper.TotalScore,
                PassScore = examPaper.PassScore,
                Duration = examPaper.Duration,
                RandomRules = examPaper.RandomRules,
                DifficultyLevel = examPaper.DifficultyLevel,
                Status = ExamPaperStatus.Draft,
                Version = 1,
                UsageCount = 0,
                AverageScore = 0,
                PassRate = 0
            };

            await _examPaperRepository.AddAsync(newExamPaper);

            // 复制试卷题目
            if (examPaper.ExamPaperQuestions.Any())
            {
                var examPaperQuestions = new List<ExamPaperQuestion>();
                foreach (var question in examPaper.ExamPaperQuestions)
                {
                    var newQuestion = new ExamPaperQuestion
                    {
                        ExamPaperId = newExamPaper.Id,
                        QuestionId = question.QuestionId,
                        QuestionVersionId = question.QuestionVersionId,
                        OrderNumber = question.OrderNumber,
                        Score = question.Score,
                        IsRequired = question.IsRequired
                    };

                    examPaperQuestions.Add(newQuestion);
                }

                await _examPaperQuestionRepository.AddRangeAsync(examPaperQuestions);
            }

            return await GetAsync(newExamPaper.Id);
        }

        /// <summary>
        /// 计算试卷难度系数
        /// </summary>
        private int CalculateDifficultyLevel(List<Question> questions)
        {
            if (questions == null || !questions.Any())
            {
                return 0;
            }

            // 根据题目难度计算试卷难度系数
            // 简单: 0-33, 中等: 34-66, 困难: 67-100
            var easyCount = questions.Count(q => q.Difficulty == QuestionDifficulty.Easy);
            var mediumCount = questions.Count(q => q.Difficulty == QuestionDifficulty.Medium);
            var hardCount = questions.Count(q => q.Difficulty == QuestionDifficulty.Hard);

            var totalCount = questions.Count;
            var difficultyLevel = (int)((easyCount * 0.2 + mediumCount * 0.5 + hardCount * 0.9) / totalCount * 100);

            return Math.Min(100, Math.Max(0, difficultyLevel));
        }

        /// <summary>
        /// 更新试卷难度
        /// </summary>
        private async Task UpdateExamPaperDifficulty(ExamPaper examPaper, List<ExamPaperQuestion> examPaperQuestions)
        {
            var questionIds = examPaperQuestions.Select(q => q.QuestionId).ToList();
            var questions = await _questionRepository
                .Find(q => questionIds.Contains(q.Id))
                .ToListAsync();

            if (questions.Any())
            {
                examPaper.DifficultyLevel = CalculateDifficultyLevel(questions);
                await _examPaperRepository.UpdateAsync(examPaper);
            }
        }

        /// <summary>
        /// 获取题目的最新版本
        /// </summary>
        /// <param name="questionId">题目ID</param>
        /// <returns>最新版本的题目</returns>
        private async Task<QuestionVersion> GetLatestQuestionVersion(long questionId)
        {
            return await _questionVersionRepository
                .Find(v => v.QuestionId == questionId)
                .OrderByDescending(v => v.Version)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 标记试卷已预览
        /// </summary>
        public async Task MarkPreviewedAsync(long id)
        {
            var examPaper = await _examPaperRepository.GetByIdAsync(id);
            if (examPaper == null)
            {
                throw new AppServiceException(404, "试卷不存在");
            }

            // 标记试卷为已预览
            examPaper.IsPreviewChecked = true;
            await _examPaperRepository.UpdateAsync(examPaper);
            
            _logger.LogInformation("试卷 {ExamPaperId} 已完成预览检查", id);
        }
    }
}