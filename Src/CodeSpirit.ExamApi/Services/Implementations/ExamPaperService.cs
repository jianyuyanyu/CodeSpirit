using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.ExamApi.Services.Interfaces;
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

        /// <summary>
        /// 构造函数
        /// </summary>
        public ExamPaperService(
            IRepository<ExamPaper> examPaperRepository,
            IRepository<ExamPaperQuestion> examPaperQuestionRepository,
            IRepository<Question> questionRepository,
            IRepository<QuestionVersion> questionVersionRepository,
            IMapper mapper,
            ILogger<ExamPaperService> logger)
            : base(examPaperRepository, mapper)
        {
            _examPaperRepository = examPaperRepository;
            _examPaperQuestionRepository = examPaperQuestionRepository;
            _questionRepository = questionRepository;
            _questionVersionRepository = questionVersionRepository;
            _mapper = mapper;
            _logger = logger;
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
        public async Task<PageList<ExamPaperDto>> GetPagedListAsync(ExamPaperQueryDto queryDto)
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
            
            // 计算难度系数
            if (createDto.Questions != null && createDto.Questions.Any())
            {
                var questionIds = createDto.Questions.Select(q => q.QuestionId).ToList();
                var questions = await _questionRepository
                    .Find(q => questionIds.Contains(q.Id))
                    .ToListAsync();
                
                if (questions.Any())
                {
                    // 根据题目难度计算试卷难度系数
                    examPaper.DifficultyLevel = CalculateDifficultyLevel(questions);
                }
            }
            
            await _examPaperRepository.AddAsync(examPaper);
            
            // 添加试卷题目
            if (createDto.Questions != null && createDto.Questions.Any())
            {
                var examPaperQuestions = new List<ExamPaperQuestion>();
                foreach (var questionDto in createDto.Questions)
                {
                    var examPaperQuestion = _mapper.Map<ExamPaperQuestion>(questionDto);
                    examPaperQuestion.ExamPaperId = examPaper.Id;
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
            
            // 更新题目列表
            if (updateDto.Questions != null)
            {
                // 删除原有题目
                await _examPaperQuestionRepository.ExecuteInTransactionAsync(async () =>
                {
                    foreach (var question in examPaper.ExamPaperQuestions)
                    {
                        await _examPaperQuestionRepository.DeleteAsync(question);
                    }
                    
                    // 添加新题目
                    var examPaperQuestions = new List<ExamPaperQuestion>();
                    foreach (var questionDto in updateDto.Questions)
                    {
                        var examPaperQuestion = _mapper.Map<ExamPaperQuestion>(questionDto);
                        examPaperQuestion.ExamPaperId = examPaper.Id;
                        examPaperQuestions.Add(examPaperQuestion);
                    }
                    
                    await _examPaperQuestionRepository.AddRangeAsync(examPaperQuestions, false);
                });
                
                // 计算难度系数
                var questionIds = updateDto.Questions.Select(q => q.QuestionId).ToList();
                var questions = await _questionRepository
                    .Find(q => questionIds.Contains(q.Id))
                    .ToListAsync();
                
                if (questions.Any())
                {
                    examPaper.DifficultyLevel = CalculateDifficultyLevel(questions);
                }
            }
            
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
            if (createDto.QuestionTypeRules == null || !createDto.QuestionTypeRules.Any())
            {
                throw new AppServiceException(400, "题型规则不能为空");
            }

            // 计算总分，确保与规则一致
            var totalScoreFromRules = createDto.QuestionTypeRules.Sum(r => r.Count * r.ScorePerQuestion);
            if (totalScoreFromRules != createDto.TotalScore)
            {
                throw new AppServiceException(400, "题型规则的总分与设置的总分不一致");
            }

            // 创建随机试卷基本信息
            var examPaper = new ExamPaper
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

            // 保存随机规则
            var randomRules = new
            {
                QuestionTypeRules = createDto.QuestionTypeRules,
                DifficultyRules = createDto.DifficultyRules,
                //KnowledgePointRules = createDto.KnowledgePointRules,
                CategoryIds = createDto.CategoryIds
            };
            examPaper.RandomRules = JsonSerializer.Serialize(randomRules);

            // 创建试卷
            await _examPaperRepository.AddAsync(examPaper);

            // 根据规则随机选择题目
            var examPaperQuestions = new List<ExamPaperQuestion>();
            var orderNumber = 1;

            foreach (var typeRule in createDto.QuestionTypeRules)
            {
                // 构建查询
                var questionQuery = _questionRepository.Find(q => q.Type == typeRule.QuestionType);

                // 应用分类过滤
                if (createDto.CategoryIds != null && createDto.CategoryIds.Any())
                {
                    questionQuery = questionQuery.Where(q => createDto.CategoryIds.Contains(q.CategoryId));
                }

                // 应用难度过滤
                if (createDto.DifficultyRules != null && createDto.DifficultyRules.Any())
                {
                    var difficulties = createDto.DifficultyRules
                        .Where(r => r.Percentage > 0)
                        .Select(r => r.Difficulty)
                        .ToList();
                    
                    if (difficulties.Any())
                    {
                        questionQuery = questionQuery.Where(q => difficulties.Contains(q.Difficulty));
                    }
                }

                //// 应用知识点过滤
                //if (createDto.KnowledgePointRules != null && createDto.KnowledgePointRules.Any())
                //{
                //    var knowledgePoints = createDto.KnowledgePointRules
                //        .Where(r => r.Percentage > 0)
                //        .Select(r => r.KnowledgePoint)
                //        .ToList();
                    
                //    if (knowledgePoints.Any())
                //    {
                //        questionQuery = questionQuery.Where(q => 
                //            knowledgePoints.Any(kp => q.KnowledgePoints != null && q.KnowledgePoints.Contains(kp)));
                //    }
                //}

                // 随机选择题目
                var randomQuestions = await questionQuery
                    .OrderBy(q => Guid.NewGuid())
                    .Take(typeRule.Count)
                    .ToListAsync();

                // 如果题目不足
                if (randomQuestions.Count < typeRule.Count)
                {
                    _logger.LogWarning("随机试卷生成时题目不足，需要{0}题，实际只有{1}题", typeRule.Count, randomQuestions.Count);
                    throw new AppServiceException(400, $"类型为{typeRule.QuestionType}的题目不足，需要{typeRule.Count}题，实际只有{randomQuestions.Count}题");
                }

                // 获取题目的最新版本
                foreach (var question in randomQuestions)
                {
                    var latestVersion = await _questionVersionRepository
                        .Find(v => v.QuestionId == question.Id)
                        .OrderByDescending(v => v.Version)
                        .FirstOrDefaultAsync();

                    if (latestVersion == null)
                    {
                        _logger.LogWarning("题目{0}没有版本信息", question.Id);
                        continue;
                    }

                    var examPaperQuestion = new ExamPaperQuestion
                    {
                        ExamPaperId = examPaper.Id,
                        QuestionId = question.Id,
                        QuestionVersionId = latestVersion.Id,
                        OrderNumber = orderNumber++,
                        Score = typeRule.ScorePerQuestion,
                        IsRequired = true
                    };

                    examPaperQuestions.Add(examPaperQuestion);
                }
            }

            // 添加试卷题目
            if (examPaperQuestions.Any())
            {
                await _examPaperQuestionRepository.AddRangeAsync(examPaperQuestions);

                // 计算难度系数
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

            return await GetAsync(examPaper.Id);
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
    }
} 