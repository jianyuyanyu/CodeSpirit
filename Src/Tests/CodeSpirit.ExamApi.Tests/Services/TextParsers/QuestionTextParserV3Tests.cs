using Xunit;
using CodeSpirit.ExamApi.Services.TextParsers.v3;
using CodeSpirit.ExamApi.Data.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodeSpirit.ExamApi.Tests.Services.TextParsers;

public class QuestionTextParserV3Tests
{
    private readonly QuestionTextParserV3 _parser;

    public QuestionTextParserV3Tests()
    {
        var loggerMock = new Mock<ILogger<QuestionTextParserV3>>();
        var singleChoiceLoggerMock = new Mock<ILogger<SingleChoiceQuestionParser>>();
        var multipleChoiceLoggerMock = new Mock<ILogger<MultipleChoiceQuestionParser>>();
        var trueFalseLoggerMock = new Mock<ILogger<TrueFalseQuestionParser>>();

        var parsers = new IQuestionParser[]
        {
            new SingleChoiceQuestionParser(singleChoiceLoggerMock.Object),
            new MultipleChoiceQuestionParser(multipleChoiceLoggerMock.Object),
            new TrueFalseQuestionParser(trueFalseLoggerMock.Object)
        };
        _parser = new QuestionTextParserV3(loggerMock.Object, parsers);
    }

    [Fact]
    public void Parse_SingleChoiceQuestion_ReturnsCorrectResult()
    {
        // Arrange
        var text = @"1、以下哪个不是C#的基本数据类型？
A、int
B、string
C、bool
D、array
(B)
解析：C#的基本数据类型包括int、string、bool等，array不是基本数据类型。
标签：C#、数据类型
难度：简单";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Single(result);
        var question = result[0];
        Assert.Equal(QuestionType.SingleChoice, question.Type);
        Assert.Equal("以下哪个不是C#的基本数据类型？", question.Content.Trim());
        Assert.Equal("string", question.CorrectAnswer);
        Assert.Equal("C#、数据类型", string.Join("、", question.Tags));
        Assert.Equal(QuestionDifficulty.Easy, question.Difficulty);
    }

    [Fact]
    public void Parse_MultipleChoiceQuestion_ReturnsCorrectResult()
    {
        // Arrange
        var text = @"2、以下哪些是C#的访问修饰符？
A、public
B、private
C、protected
D、internal
(ABC)
解析：C#的访问修饰符包括public、private、protected和internal。
标签：C#、访问修饰符
难度：中等";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Single(result);
        var question = result[0];
        Assert.Equal(QuestionType.MultipleChoice, question.Type);
        Assert.Equal("以下哪些是C#的访问修饰符？", question.Content.Trim());
        Assert.Equal("public;private;protected", question.CorrectAnswer);
        Assert.Equal("C#、访问修饰符", string.Join("、", question.Tags));
        Assert.Equal(QuestionDifficulty.Medium, question.Difficulty);
    }

    [Fact]
    public void Parse_TrueFalseQuestion_ReturnsCorrectResult()
    {
        // Arrange
        var text = @"3、C#是一种强类型语言。
（对）
解析：C#是一种强类型语言，这意味着所有的变量和对象都必须有明确的类型。
标签：C#、语言特性
难度：困难";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Single(result);
        var question = result[0];
        Assert.Equal(QuestionType.TrueFalse, question.Type);
        Assert.Equal("C#是一种强类型语言。", question.Content.Trim());
        Assert.Equal("True", question.CorrectAnswer);
        Assert.Equal("C#、语言特性", string.Join("、", question.Tags));
        Assert.Equal(QuestionDifficulty.Hard, question.Difficulty);
    }

    [Fact]
    public void Parse_SingleChoiceWithCodeSnippet_ReturnsCorrectResult()
    {
        // Arrange
        var text = @"4、以下代码的输出结果是什么？
```csharp
int x = 5;
int y = 2;
Console.WriteLine(x / y);
```
A、2
B、2.5
C、2.0
D、3
(A)
解析：在C#中，整数相除会得到整数结果，5除以2等于2。
标签：C#、运算符
难度：简单";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Single(result);
        var question = result[0];
        Assert.Equal(QuestionType.SingleChoice, question.Type);
        Assert.Equal("以下代码的输出结果是什么？", question.Content.Trim());
        Assert.Equal("2", question.CorrectAnswer);
        Assert.Equal("C#、运算符", string.Join("、", question.Tags));
        Assert.Equal(QuestionDifficulty.Easy, question.Difficulty);
    }

    [Fact]
    public void Parse_QuestionsWithDifficulty_ReturnsCorrectResults()
    {
        // Arrange
        var text = @"1、以下哪个不是C#的基本数据类型？
A、int
B、string
C、bool
D、array
(B)
解析：C#的基本数据类型包括int、string、bool等，array不是基本数据类型。
标签：C#、数据类型
难度：简单

2、以下哪些是C#的访问修饰符？
A、public
B、private
C、protected
D、internal
(ABC)
解析：C#的访问修饰符包括public、private、protected和internal。
标签：C#、访问修饰符
难度：中等

3、C#是一种强类型语言。
（对）
解析：C#是一种强类型语言，这意味着所有的变量和对象都必须有明确的类型。
标签：C#、语言特性
难度：困难";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(QuestionDifficulty.Easy, result[0].Difficulty);
        Assert.Equal(QuestionDifficulty.Medium, result[1].Difficulty);
        Assert.Equal(QuestionDifficulty.Hard, result[2].Difficulty);
    }

    [Fact]
    public void Parse_SingleChoiceWithCodeSnippet_ReturnsCorrectResult2()
    {
        // Arrange
        var text = @"一、单项选择题（每题1分）
    31. int i=1; int  j=10;  
    do{                    
       		 if(i> j) {         
    break; 
    }
    i=i+2;  
    j=j-1;
    	}while(i<10); 
    System.out.println(i+""\t""+j);
    执行完毕后，i和j的值分别是 （  B    ）。
    A. i=5   j=8 
    B. i=9   j=6        
    C. i=6   j=9
    D. i=8   j=5
    【解析】代码执行过程：第一次循环i=3,j=9；第二次i=5,j=8；第三次i=7,j=7；第四次i=9,j=6，此时i<10条件仍满足但i>j成立，触发break跳出循环。因此最终i=9,j=6。
    【标签】循环、break语句";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Single(result);
        var question = result[0];
        Assert.Equal(QuestionType.SingleChoice, question.Type);

        Assert.Contains("int i=1; int  j=10;", question.Content);
        Assert.Contains("break;", question.Content);
        Assert.Contains("System.out.println(i+\"\\t\"+j);", question.Content);
        Assert.Contains("执行完毕后，i和j的值分别是", question.Content);

        Assert.Equal("i=9   j=6", question.CorrectAnswer);
        Assert.Equal(4, question.Options.Count);
        Assert.Equal("代码执行过程：第一次循环i=3,j=9；第二次i=5,j=8；第三次i=7,j=7；第四次i=9,j=6，此时i<10条件仍满足但i>j成立，触发break跳出循环。因此最终i=9,j=6。", question.Analysis);
        Assert.Equal(2, question.Tags.Count);
        Assert.Contains("循环", question.Tags);
        Assert.Contains("break语句", question.Tags);
    }

    [Fact]
    public void Parse_ECommerceQuestions_ReturnsCorrectResults()
    {
        // Arrange
        var text = @"一、单项选择题（每题1分，2小题，共计10分）

1. 电商平台中，最常用的支付方式是（B）。
A、现金支付  
B、在线支付  
C、货到付款  
D、银行转账  
【解析】在线支付是目前电商平台最常用的支付方式，方便快捷。  
【标签】支付方式、在线支付

2. 以下哪种电商模式主要依靠第三方平台来完成交易？（C）
A、B2B  
B、C2C  
C、B2C  
D、C2B  
【解析】C2C（Consumer to Consumer）是指消费者之间通过第三方平台完成交易。  
【标签】电商模式、C2C";

        var text2 = @"二、判断题（每空2分，2小题，共计20分）

1. 电商平台中，消费者提交订单后，若在规定时间内未付款，则订单会被自动取消。（对）  
【解析】大部分电商平台会在规定时间内未付款的订单自动取消。  
【标签】订单、取消

2. 跨境电商可以直接通过国内电商平台进行购物，无需关税。（错）  
【解析】跨境电商需要遵循各国的关税政策，不同国家的关税政策不同。  
【标签】跨境电商、关税";

        // Act
        var singleChoiceResult = _parser.Parse(text);
        var trueFalseResult = _parser.Parse(text2);

        // Assert - 单选题
        Assert.Equal(2, singleChoiceResult.Count);
        var question1 = singleChoiceResult[0];
        var question2 = singleChoiceResult[1];

        // 验证第一道单选题
        Assert.Equal(QuestionType.SingleChoice, question1.Type);
        Assert.Equal("电商平台中，最常用的支付方式是。", question1.Content);
        Assert.Equal("在线支付", question1.CorrectAnswer);
        Assert.Equal(4, question1.Options.Count);
        Assert.Equal("在线支付是目前电商平台最常用的支付方式，方便快捷。", question1.Analysis);
        Assert.Equal(2, question1.Tags.Count);
        Assert.Contains("支付方式", question1.Tags);
        Assert.Contains("在线支付", question1.Tags);

        // 验证第二道单选题
        Assert.Equal(QuestionType.SingleChoice, question2.Type);
        Assert.Equal("以下哪种电商模式主要依靠第三方平台来完成交易？", question2.Content);
        Assert.Equal("B2C", question2.CorrectAnswer);
        Assert.Equal(4, question2.Options.Count);
        Assert.Equal("C2C（Consumer to Consumer）是指消费者之间通过第三方平台完成交易。", question2.Analysis);
        Assert.Equal(2, question2.Tags.Count);
        Assert.Contains("电商模式", question2.Tags);
        Assert.Contains("C2C", question2.Tags);

        // Assert - 判断题
        Assert.Equal(2, trueFalseResult.Count);
        var tfQuestion1 = trueFalseResult[0];
        var tfQuestion2 = trueFalseResult[1];

        // 验证第一道判断题
        Assert.Equal(QuestionType.TrueFalse, tfQuestion1.Type);
        Assert.Equal("电商平台中，消费者提交订单后，若在规定时间内未付款，则订单会被自动取消。", tfQuestion1.Content);
        Assert.Equal("True", tfQuestion1.CorrectAnswer);
        Assert.Equal("大部分电商平台会在规定时间内未付款的订单自动取消。", tfQuestion1.Analysis);
        Assert.Equal(2, tfQuestion1.Tags.Count);
        Assert.Contains("订单", tfQuestion1.Tags);
        Assert.Contains("取消", tfQuestion1.Tags);

        // 验证第二道判断题
        Assert.Equal(QuestionType.TrueFalse, tfQuestion2.Type);
        Assert.Equal("跨境电商可以直接通过国内电商平台进行购物，无需关税。", tfQuestion2.Content);
        Assert.Equal("False", tfQuestion2.CorrectAnswer);
        Assert.Equal("跨境电商需要遵循各国的关税政策，不同国家的关税政策不同。", tfQuestion2.Analysis);
        Assert.Equal(2, tfQuestion2.Tags.Count);
        Assert.Contains("跨境电商", tfQuestion2.Tags);
        Assert.Contains("关税", tfQuestion2.Tags);
    }

    [Fact]
    public void Parse_Complete20Questions_ReturnsCorrectResults()
    {
        // Arrange
        var singleChoiceText = @"一、单项选择题（每题1分，10小题，共计10分）

1. 电商平台中，最常用的支付方式是（B）。
A、现金支付  
B、在线支付  
C、货到付款  
D、银行转账  
【解析】在线支付是目前电商平台最常用的支付方式，方便快捷。  
【标签】支付方式、在线支付

2. 以下哪种电商模式主要依靠第三方平台来完成交易？（C）
A、B2B  
B、C2C  
C、B2C  
D、C2B  
【解析】C2C（Consumer to Consumer）是指消费者之间通过第三方平台完成交易。  
【标签】电商模式、C2C

3. 在电商网站上，客户未付款且未确认的订单状态通常被称为（D）。  
A、已完成  
B、已发货  
C、待处理  
D、待支付  
【解析】待支付表示客户未付款且未确认的订单。  
【标签】订单状态、待支付

4. 以下哪种方式最能有效提高电商平台的用户粘性？（A）  
A、积分制度  
B、广告投放  
C、促销活动  
D、优化物流配送  
【解析】积分制度能够鼓励用户持续消费，增强用户粘性。  
【标签】用户粘性、积分

5. 电商平台中的限时抢购通常属于哪种促销方式？（C）  
A、满减  
B、打折  
C、秒杀  
D、返现  
【解析】限时抢购通常指秒杀促销，用户在限定时间内抢购商品。  
【标签】促销方式、秒杀

6. 在电商营销中，常用的A/B测试主要是为了（D）。  
A、增加广告费用  
B、提高平台用户数量  
C、增加平台运营成本  
D、优化用户体验  
【解析】A/B测试通过对比两种版本，优化用户体验，提高转化率。  
【标签】A/B测试、用户体验

7. 以下哪个不是电子商务中的常见支付工具？（B）  
A、支付宝  
B、现金  
C、微信支付  
D、信用卡  
【解析】现金不是电商支付中常见的支付工具。  
【标签】支付工具、电商支付

8. 以下哪项不属于电商平台的核心竞争力？（D）  
A、物流配送能力  
B、支付方式多样性  
C、商品种类丰富  
D、客服人员数量  
【解析】客服人员数量虽然重要，但不是电商平台的核心竞争力。  
【标签】核心竞争力、电商平台

9. 以下哪种方式是电商平台常用的用户获取策略？（A）  
A、社交媒体营销  
B、邮件营销  
C、短信营销  
D、电视广告  
【解析】社交媒体营销是一种通过社交平台快速增加用户的方式。  
【标签】用户获取、社交媒体

10. 电商平台中的黑五指的是哪个节日？（C）  
A、圣诞节  
B、万圣节  
C、感恩节后购物狂欢日  
D、情人节  
【解析】黑五即美国感恩节后的一天，购物活动非常火爆。  
【标签】节日促销、黑五";

        var trueFalseText = @"二、判断题（每题2分，10小题，共计20分）

1. 电商平台中，消费者提交订单后，若在规定时间内未付款，则订单会被自动取消。（对）  
【解析】大部分电商平台会在规定时间内未付款的订单自动取消。  
【标签】订单、取消

2. 跨境电商可以直接通过国内电商平台进行购物，无需关税。（错）  
【解析】跨境电商需要遵循各国的关税政策，不同国家的关税政策不同。  
【标签】跨境电商、关税

3. 电商平台上，消费者的评价系统对于商家来说没有任何影响。（错）  
【解析】消费者评价对商家的声誉和销量有重要影响。  
【标签】评价系统、商家

4. 电商平台的推荐算法仅依赖于用户的购买历史。（错）  
【解析】推荐算法除了用户购买历史外，还会考虑浏览记录、偏好等因素。  
【标签】推荐算法、用户行为

5. 线上商店无需考虑线下门店的库存管理。（错）  
【解析】即使是线上商店，库存管理仍然是非常关键的一环。  
【标签】库存管理、线上商店

6. 快递服务是电商平台物流配送的重要组成部分。（对）  
【解析】快递服务直接影响商品的配送速度和客户的满意度。  
【标签】物流配送、快递

7. 电商平台的流量来源仅依赖于广告投放。（错）  
【解析】流量来源包括广告投放、社交媒体、搜索引擎优化等多个渠道。  
【标签】流量来源、电商平台

8. 用户在电商平台购物时，价格始终是唯一影响购买决策的因素。（错）  
【解析】除了价格，商品质量、评价、配送速度等也会影响购买决策。  
【标签】购买决策、影响因素

9. 移动电商的发展使得消费者购物变得更加便捷。（对）  
【解析】移动电商让消费者可以随时随地进行购物。  
【标签】移动电商、便捷

10. 电子发票在电商平台中是商家根据用户要求提供的。（对）  
【解析】电子发票通常是根据用户要求开具的，可以用于报销等目的。  
【标签】电子发票、电商";

        // Act
        var singleChoiceResult = _parser.Parse(singleChoiceText);
        var trueFalseResult = _parser.Parse(trueFalseText);

        // Assert
        // 验证题目数量
        Assert.Equal(10, singleChoiceResult.Count);
        Assert.Equal(10, trueFalseResult.Count);

        // 验证单选题
        foreach (var question in singleChoiceResult)
        {
            Assert.Equal(QuestionType.SingleChoice, question.Type);
            Assert.Equal(4, question.Options.Count);
            Assert.NotEmpty(question.Content);
            Assert.NotEmpty(question.CorrectAnswer);
            Assert.NotEmpty(question.Analysis);
            Assert.Equal(2, question.Tags.Count);
        }

        // 验证判断题
        foreach (var question in trueFalseResult)
        {
            Assert.Equal(QuestionType.TrueFalse, question.Type);
            Assert.Equal(2, question.Options.Count);
            Assert.NotEmpty(question.Content);
            Assert.Contains(question.CorrectAnswer, new[] { "True", "False" });
            Assert.NotEmpty(question.Analysis);
            Assert.Equal(2, question.Tags.Count);
        }

        // 验证具体题目内容（以第一题为例）
        var firstSingleChoice = singleChoiceResult[0];
        Assert.Equal("电商平台中，最常用的支付方式是。", firstSingleChoice.Content);
        Assert.Equal("在线支付", firstSingleChoice.CorrectAnswer);
        Assert.Equal("在线支付是目前电商平台最常用的支付方式，方便快捷。", firstSingleChoice.Analysis);
        Assert.Contains("支付方式", firstSingleChoice.Tags);
        Assert.Contains("在线支付", firstSingleChoice.Tags);

        var firstTrueFalse = trueFalseResult[0];
        Assert.Equal("电商平台中，消费者提交订单后，若在规定时间内未付款，则订单会被自动取消。", firstTrueFalse.Content);
        Assert.Equal("True", firstTrueFalse.CorrectAnswer);
        Assert.Equal("大部分电商平台会在规定时间内未付款的订单自动取消。", firstTrueFalse.Analysis);
        Assert.Contains("订单", firstTrueFalse.Tags);
        Assert.Contains("取消", firstTrueFalse.Tags);

        // 验证判断题的正确答案
        var expectedTrueFalseAnswers = new[] { "True", "False", "False", "False", "False", "True", "False", "False", "True", "True" };
        Assert.Equal(expectedTrueFalseAnswers[2], trueFalseResult[2].CorrectAnswer);
        Assert.Equal(expectedTrueFalseAnswers[7], trueFalseResult[7].CorrectAnswer);
        for (int i = 0; i < trueFalseResult.Count; i++)
        {
            Assert.Equal(expectedTrueFalseAnswers[i], trueFalseResult[i].CorrectAnswer);
        }

        // 验证单选题的正确答案
        var expectedSingleChoiceAnswers = new[] { "在线支付", "B2C", "待支付", "积分制度", "秒杀", "优化用户体验", "现金", "客服人员数量", "社交媒体营销", "感恩节后购物狂欢日" };
        for (int i = 0; i < singleChoiceResult.Count; i++)
        {
            Assert.Equal(expectedSingleChoiceAnswers[i], singleChoiceResult[i].CorrectAnswer);
        }
    }
}