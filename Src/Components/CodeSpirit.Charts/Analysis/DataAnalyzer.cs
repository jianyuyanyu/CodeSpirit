using System.Collections;
using CodeSpirit.Core.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.Charts.Analysis
{
    /// <summary>
    /// 数据分析器实现
    /// </summary>
    public class DataAnalyzer : IDataAnalyzer, ISingletonDependency
    {
        private readonly ILogger<DataAnalyzer> _logger;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public DataAnalyzer(ILogger<DataAnalyzer> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// 分析数据结构
        /// </summary>
        public DataStructureInfo AnalyzeDataStructure(object data)
        {
            var result = new DataStructureInfo
            {
                FieldTypes = new Dictionary<string, Type>(),
                FieldSamples = new Dictionary<string, object?>(),
                DimensionFields = new List<string>(),
                MetricFields = new List<string>()
            };

            try
            {
                var jsonData = JsonConvert.SerializeObject(data);
                var jsonObject = JToken.Parse(jsonData);

                if (jsonObject is JArray jsonArray)
                {
                    // 处理数组数据
                    if (jsonArray.Count > 0)
                    {
                        var firstItem = jsonArray[0];
                        // 分析第一项确定字段类型
                        AnalyzeObjectProperties(firstItem, result);
                    }

                    // 记录行数
                    result.RowCount = jsonArray.Count;

                    // 收集样本值
                    CollectSampleValues(jsonArray, result);
                }
                else if (jsonObject is JObject jObject)
                {
                    // 处理对象数据 - 寻找可能的数据数组属性
                    var dataArrayProperty = FindDataArrayProperty(jObject);
                    if (dataArrayProperty != null && dataArrayProperty is JArray arrayProperty)
                    {
                        // 记录数据所在的数组位置 (不再写入DataPropertyName，仅作为日志记录)
                        if (dataArrayProperty.Path.Contains("."))
                        {
                            var pathParts = dataArrayProperty.Path.Split('.');
                            var propertyName = pathParts[pathParts.Length - 1];
                            _logger.LogDebug($"发现数据数组属性: {propertyName}");
                        }
                        
                        if (arrayProperty.Count > 0)
                        {
                            var firstItem = arrayProperty[0];
                            AnalyzeObjectProperties(firstItem, result);
                        }

                        result.RowCount = arrayProperty.Count;
                        CollectSampleValues(arrayProperty, result);
                    }
                    else
                    {
                        // 如果不是数组属性，就直接分析对象属性
                        AnalyzeObjectProperties(jObject, result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析数据结构时出错");
            }

            return result;
        }
        
        /// <summary>
        /// 提取数据特征
        /// </summary>
        public DataFeatures ExtractDataFeatures(object data)
        {
            try
            {
                // 获取数据结构信息
                var structureInfo = AnalyzeDataStructure(data);
                
                var features = new DataFeatures
                {
                    MetricStatistics = new Dictionary<string, MetricStats>()
                };
                
                var jsonData = JsonConvert.SerializeObject(data);
                var jsonObject = JToken.Parse(jsonData);
                var dataArray = FindDataArray(jsonObject);
                
                // 找到数据数组
                if (dataArray is JArray dataArrayToken && dataArrayToken.Count == 0)
                {
                    return features;
                }
                
                // 分析每个指标字段的统计信息
                foreach (var metricField in structureInfo.MetricFields)
                {
                    var values = ExtractNumericValues(dataArray, metricField);
                    if (values.Count > 0)
                    {
                        features.MetricStatistics[metricField] = CalculateStatistics(values);
                    }
                }
                
                // 检测是否为时间序列
                features.IsTimeSeries = DetectTimeSeriesData(dataArray, structureInfo.DimensionFields);
                
                // 简单的趋势检测
                features.HasTrend = DetectTrend(dataArray, structureInfo.DimensionFields, structureInfo.MetricFields);
                
                // 分类与连续特性检测
                features.IsCategorical = structureInfo.DimensionFields.Count > 0 && 
                                          structureInfo.DimensionFields.Any(f => IsStringField(f, structureInfo.FieldTypes));
                
                features.IsContinuous = structureInfo.MetricFields.Count > 0;
                
                // 异常值检测
                features.HasOutliers = DetectOutliers(features.MetricStatistics);
                
                return features;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提取数据特征时出错");
                return new DataFeatures();
            }
        }
        
        /// <summary>
        /// 检测数据关联性
        /// </summary>
        public List<DataCorrelation> DetectCorrelations(object data)
        {
            var correlations = new List<DataCorrelation>();
            
            try
            {
                var structureInfo = AnalyzeDataStructure(data);
                
                // 如果指标字段少于2个，无法计算相关性
                if (structureInfo.MetricFields.Count < 2)
                {
                    return correlations;
                }
                
                var jsonData = JsonConvert.SerializeObject(data);
                var jsonObject = JToken.Parse(jsonData);
                var dataArray = FindDataArray(jsonObject);
                
                // 计算每对指标字段之间的相关性
                for (int i = 0; i < structureInfo.MetricFields.Count; i++)
                {
                    for (int j = i + 1; j < structureInfo.MetricFields.Count; j++)
                    {
                        var field1 = structureInfo.MetricFields[i];
                        var field2 = structureInfo.MetricFields[j];
                        
                        var values1 = ExtractNumericValues(dataArray, field1);
                        var values2 = ExtractNumericValues(dataArray, field2);
                        
                        if (values1.Count > 0 && values2.Count > 0 && values1.Count == values2.Count)
                        {
                            // 计算皮尔逊相关系数（示例简化实现）
                            var correlation = CalculateCorrelation(values1, values2);
                            
                            correlations.Add(new DataCorrelation
                            {
                                Field1 = field1,
                                Field2 = field2,
                                Coefficient = correlation,
                                Strength = DescribeCorrelationStrength(correlation)
                            });
                        }
                    }
                }
                
                // 按相关系数绝对值排序
                correlations = correlations.OrderByDescending(c => Math.Abs(c.Coefficient)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检测数据关联性时出错");
            }
            
            return correlations;
        }
        
        /// <summary>
        /// 识别数据模式
        /// </summary>
        public List<DataPattern> IdentifyPatterns(object data)
        {
            var patterns = new List<DataPattern>();
            
            try
            {
                var structureInfo = AnalyzeDataStructure(data);
                var features = ExtractDataFeatures(data);
                
                // 识别时间序列趋势
                if (features.IsTimeSeries && features.HasTrend)
                {
                    patterns.Add(new DataPattern
                    {
                        Type = "TimeTrend",
                        Description = "数据显示明显的时间趋势",
                        Confidence = 0.8,
                        RelatedFields = structureInfo.DimensionFields.Concat(structureInfo.MetricFields).ToList()
                    });
                }
                
                // 识别分类分布
                if (features.IsCategorical && structureInfo.DimensionFields.Count > 0)
                {
                    patterns.Add(new DataPattern
                    {
                        Type = "CategoryDistribution",
                        Description = "数据表现为分类分布特性",
                        Confidence = 0.75,
                        RelatedFields = structureInfo.DimensionFields.ToList()
                    });
                }
                
                // 识别异常值模式
                if (features.HasOutliers)
                {
                    var outlierFields = features.MetricStatistics
                        .Where(kv => HasOutliers(kv.Value))
                        .Select(kv => kv.Key)
                        .ToList();
                    
                    if (outlierFields.Count > 0)
                    {
                        patterns.Add(new DataPattern
                        {
                            Type = "Outliers",
                            Description = "数据中存在明显异常值",
                            Confidence = 0.7,
                            RelatedFields = outlierFields
                        });
                    }
                }
                
                // 检测关联模式
                var correlations = DetectCorrelations(data);
                var strongCorrelations = correlations.Where(c => Math.Abs(c.Coefficient) > 0.7).ToList();
                
                if (strongCorrelations.Count > 0)
                {
                    patterns.Add(new DataPattern
                    {
                        Type = "StrongCorrelation",
                        Description = "字段之间存在强相关性",
                        Confidence = 0.85,
                        RelatedFields = strongCorrelations.SelectMany(c => new[] { c.Field1, c.Field2 }).Distinct().ToList()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "识别数据模式时出错");
            }
            
            return patterns;
        }
        
        #region 辅助方法
        
        /// <summary>
        /// 分析对象属性
        /// </summary>
        private void AnalyzeObjectProperties(JToken token, DataStructureInfo result)
        {
            if (token is JObject jObject)
            {
                foreach (var property in jObject.Properties())
                {
                    string name = property.Name;
                    Type type = DetermineType(property.Value);
                    
                    // 添加到结果中
                    if (!result.FieldTypes.ContainsKey(name))
                    {
                        result.FieldTypes.Add(name, type);
                        result.FieldSamples[name] = null;  // 初始化字段样本
                        
                        // 根据类型确定是维度还是指标
                        if (IsNumericType(type))
                        {
                            result.MetricFields.Add(name);
                        }
                        else
                        {
                            result.DimensionFields.Add(name);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 收集样本值
        /// </summary>
        private void CollectSampleValues(JArray array, DataStructureInfo result)
        {
            // 从每个项收集样本值
            int sampleSize = Math.Min(array.Count, 10); // 最多收集10个样本
            
            for (int i = 0; i < sampleSize; i++)
            {
                if (array[i] is JObject jObject)
                {
                    foreach (var property in jObject.Properties())
                    {
                        string name = property.Name;
                        if (result.FieldTypes.ContainsKey(name))
                        {
                            object? value = GetValueObject(property.Value);
                            if (value != null)
                            {
                                // 存储第一个非空样本值
                                if (result.FieldSamples[name] == null)
                                {
                                    result.FieldSamples[name] = value;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取值对象
        /// </summary>
        private object? GetValueObject(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.String:
                    return token.Value<string>();
                case JTokenType.Integer:
                    return token.Value<long>();
                case JTokenType.Float:
                    return token.Value<double>();
                case JTokenType.Boolean:
                    return token.Value<bool>();
                case JTokenType.Date:
                    return token.Value<DateTime>();
                case JTokenType.Null:
                    return null;
                default:
                    return token.ToString();
            }
        }
        
        /// <summary>
        /// 确定字段类型
        /// </summary>
        private Type DetermineType(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.String:
                    // 尝试检测日期类型
                    if (DateTime.TryParse(token.Value<string>(), out _))
                    {
                        return typeof(DateTime);
                    }
                    return typeof(string);
                case JTokenType.Integer:
                    return typeof(long);
                case JTokenType.Float:
                    return typeof(double);
                case JTokenType.Boolean:
                    return typeof(bool);
                case JTokenType.Date:
                    return typeof(DateTime);
                case JTokenType.Array:
                    return typeof(Array);
                case JTokenType.Object:
                    return typeof(object);
                default:
                    return typeof(string);
            }
        }
        
        /// <summary>
        /// 判断是否为数值类型
        /// </summary>
        private bool IsNumericType(Type type)
        {
            return type == typeof(int) ||
                   type == typeof(long) ||
                   type == typeof(float) ||
                   type == typeof(double) ||
                   type == typeof(decimal);
        }
        
        /// <summary>
        /// 查找数据数组属性
        /// </summary>
        private JToken FindDataArrayProperty(JToken token)
        {
            if (token is JObject jObject)
            {
                // 寻找最大的数组属性
                JToken? largestArray = null;
                int maxCount = 0;
                
                foreach (var property in jObject.Properties())
                {
                    if (property.Value is JArray array && array.Count > maxCount)
                    {
                        maxCount = array.Count;
                        largestArray = property.Value;
                    }
                }
                
                return largestArray ?? new JArray();
            }
            
            return new JArray();
        }
        
        /// <summary>
        /// 查找数据数组
        /// </summary>
        private JToken FindDataArray(JToken token)
        {
            if (token is JArray array)
            {
                return array;
            }
            else if (token is JObject jObject)
            {
                // 寻找可能的数据数组属性
                return FindDataArrayProperty(jObject);
            }
            
            return new JArray();
        }
        
        /// <summary>
        /// 提取数值字段值
        /// </summary>
        private List<double> ExtractNumericValues(JToken dataArray, string fieldName)
        {
            var values = new List<double>();
            
            if (dataArray is JArray array)
            {
                foreach (var item in array)
                {
                    if (item is JObject jObject && jObject.TryGetValue(fieldName, out var value))
                    {
                        if (value.Type == JTokenType.Integer || value.Type == JTokenType.Float)
                        {
                            values.Add(value.Value<double>());
                        }
                    }
                }
            }
            
            return values;
        }
        
        /// <summary>
        /// 计算统计信息
        /// </summary>
        private MetricStats CalculateStatistics(List<double> values)
        {
            if (values.Count == 0)
            {
                return new MetricStats();
            }
            
            var stats = new MetricStats
            {
                Min = values.Min(),
                Max = values.Max(),
                Average = values.Average()
            };
            
            // 计算中位数
            var sortedValues = values.OrderBy(v => v).ToList();
            if (sortedValues.Count % 2 == 0)
            {
                stats.Median = (sortedValues[sortedValues.Count / 2 - 1] + sortedValues[sortedValues.Count / 2]) / 2;
            }
            else
            {
                stats.Median = sortedValues[sortedValues.Count / 2];
            }
            
            // 计算标准差
            double sumOfSquares = values.Sum(v => Math.Pow(v - stats.Average, 2));
            stats.StdDev = Math.Sqrt(sumOfSquares / values.Count);
            
            return stats;
        }
        
        /// <summary>
        /// 判断是否为字符串字段
        /// </summary>
        private bool IsStringField(string fieldName, Dictionary<string, Type> fieldTypes)
        {
            return fieldTypes.TryGetValue(fieldName, out var type) && type == typeof(string);
        }
        
        /// <summary>
        /// 检测是否为时间序列数据
        /// </summary>
        private bool DetectTimeSeriesData(JToken dataArray, List<string> dimensionFields)
        {
            if (!(dataArray is JArray array) || array.Count < 2)
            {
                return false;
            }
            
            // 检查是否存在日期/时间类型的字段
            foreach (var field in dimensionFields)
            {
                bool isTimeField = true;
                
                // 检查前几个元素的值是否都可以解析为日期
                int checkCount = Math.Min(5, array.Count);
                for (int i = 0; i < checkCount; i++)
                {
                    if (array[i] is JObject jObject && jObject.TryGetValue(field, out var value))
                    {
                        if (value is JValue jValue && !DateTime.TryParse(jValue.Value<string>(), out _))
                        {
                            isTimeField = false;
                            break;
                        }
                    }
                    else
                    {
                        isTimeField = false;
                        break;
                    }
                }
                
                if (isTimeField)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 检测趋势
        /// </summary>
        private bool DetectTrend(JToken dataArray, List<string> dimensionFields, List<string> metricFields)
        {
            if (!(dataArray is JArray array) || array.Count < 5 || metricFields.Count == 0)
            {
                return false;
            }
            
            // 如果是时间序列，检查是否存在明显趋势
            if (DetectTimeSeriesData(dataArray, dimensionFields))
            {
                foreach (var metricField in metricFields)
                {
                    var values = ExtractNumericValues(dataArray, metricField);
                    if (values.Count >= 5)
                    {
                        // 简单线性回归检测趋势
                        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
                        int n = values.Count;
                        
                        for (int i = 0; i < n; i++)
                        {
                            sumX += i;
                            sumY += values[i];
                            sumXY += i * values[i];
                            sumX2 += i * i;
                        }
                        
                        double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
                        
                        // 如果斜率显著不为0，认为存在趋势
                        if (Math.Abs(slope) > 0.05 * (values.Max() - values.Min()) / n)
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 检测异常值
        /// </summary>
        private bool DetectOutliers(Dictionary<string, MetricStats> metricStats)
        {
            foreach (var stats in metricStats.Values)
            {
                if (HasOutliers(stats))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 判断是否存在异常值
        /// </summary>
        private bool HasOutliers(MetricStats stats)
        {
            // 使用3倍标准差作为异常值判断标准
            double lowerBound = stats.Average - 3 * stats.StdDev;
            double upperBound = stats.Average + 3 * stats.StdDev;
            
            return stats.Min < lowerBound || stats.Max > upperBound;
        }
        
        /// <summary>
        /// 计算相关系数
        /// </summary>
        private double CalculateCorrelation(List<double> values1, List<double> values2)
        {
            if (values1.Count != values2.Count || values1.Count == 0)
            {
                return 0;
            }
            
            double mean1 = values1.Average();
            double mean2 = values2.Average();
            
            double sum = 0;
            double sum1 = 0;
            double sum2 = 0;
            
            for (int i = 0; i < values1.Count; i++)
            {
                double diff1 = values1[i] - mean1;
                double diff2 = values2[i] - mean2;
                
                sum += diff1 * diff2;
                sum1 += diff1 * diff1;
                sum2 += diff2 * diff2;
            }
            
            if (sum1 == 0 || sum2 == 0)
            {
                return 0;
            }
            
            return sum / Math.Sqrt(sum1 * sum2);
        }
        
        /// <summary>
        /// 描述相关强度
        /// </summary>
        private string DescribeCorrelationStrength(double correlation)
        {
            correlation = Math.Abs(correlation);
            
            if (correlation >= 0.8)
                return "很强";
            if (correlation >= 0.6)
                return "强";
            if (correlation >= 0.4)
                return "中等";
            if (correlation >= 0.2)
                return "弱";
            
            return "很弱";
        }
        
        #endregion
    }
} 