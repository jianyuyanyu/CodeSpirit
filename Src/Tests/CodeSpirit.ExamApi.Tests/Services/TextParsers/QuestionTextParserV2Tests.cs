using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Services.TextParsers.v2;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.ExamApi.Tests.Services.TextParsers;

public class QuestionTextParserV2Tests
{
    private readonly Mock<ILogger<QuestionTextParserV2>> _loggerMock;
    private readonly QuestionTextParserV2 _parser;

    public QuestionTextParserV2Tests()
    {
        _loggerMock = new Mock<ILogger<QuestionTextParserV2>>();
        var singleChoiceLoggerMock = new Mock<ILogger<SingleChoiceQuestionParser>>();
        var multipleChoiceLoggerMock = new Mock<ILogger<MultipleChoiceQuestionParser>>();
        var trueFalseLoggerMock = new Mock<ILogger<TrueFalseQuestionParser>>();

        var singleChoiceParser = new SingleChoiceQuestionParser(singleChoiceLoggerMock.Object);
        var multipleChoiceParser = new MultipleChoiceQuestionParser(multipleChoiceLoggerMock.Object);
        var trueFalseParser = new TrueFalseQuestionParser(trueFalseLoggerMock.Object);

        _parser = new QuestionTextParserV2(_loggerMock.Object, singleChoiceParser, trueFalseParser, multipleChoiceParser);
    }

    [Fact]
    public void Parse_EmptyText_ReturnsEmptyList()
    {
        // Arrange
        var text = "";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_SingleChoiceQuestion_ReturnsCorrectResult()
    {
        // Arrange
        var text = @"一、单项选择题（每题1分）
1、SSL协议最早是由(B)提出的。
A、Microsoft
B、Netscape
C、ISO
D、IBM
【解析】SSL协议是由Netscape公司提出的，用于在互联网上提供安全通信。
【标签】SSL、安全通信";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Single(result);
        var question = result[0];
        Assert.Equal(QuestionType.SingleChoice, question.Type);
        Assert.Equal("SSL协议最早是由提出的。", question.Content);
        Assert.Equal("Netscape", question.CorrectAnswer);
        Assert.Equal(4, question.Options.Count);
        Assert.Equal("SSL协议是由Netscape公司提出的，用于在互联网上提供安全通信。", question.Analysis);
        Assert.Equal(2, question.Tags.Count);
        Assert.Contains("SSL", question.Tags);
        Assert.Contains("安全通信", question.Tags);
    }

    [Fact]
    public void Parse_TrueFalseQuestion_ReturnsCorrectResult()
    {
        // Arrange
        var text = @"二、判断题（每题1分）
1. 平邮包裹的到货周期较长，顾客通常要7 - 15天才能收到购买的商品, 但是提供了网上查询物流进程的服务。（对）
【解析】平邮包裹确实有网上查询物流进程的服务。
【标签】平邮、商品";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Single(result);
        var question = result[0];
        Assert.Equal(QuestionType.TrueFalse, question.Type);
        Assert.Equal("平邮包裹的到货周期较长，顾客通常要7 - 15天才能收到购买的商品, 但是提供了网上查询物流进程的服务。", question.Content);
        Assert.Equal("True", question.CorrectAnswer);
        Assert.Equal(2, question.Options.Count);
        Assert.Equal("平邮包裹确实有网上查询物流进程的服务。", question.Analysis);
        Assert.Equal(2, question.Tags.Count);
        Assert.Contains("平邮", question.Tags);
        Assert.Contains("商品", question.Tags);
    }

    [Fact]
    public void Parse_MultipleChoiceQuestion_ReturnsCorrectResult()
    {
        // Arrange
        var text = @"三、多项选择题（每题2分）
1、以下哪些是常见的Web安全威胁(ABC)？
A、SQL注入
B、跨站脚本攻击
C、跨站请求伪造
D、合法访问
【解析】SQL注入、XSS和CSRF都是常见的Web安全威胁。
【标签】Web安全、安全威胁";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Single(result);
        var question = result[0];
        Assert.Equal(QuestionType.MultipleChoice, question.Type);
        Assert.Equal("以下哪些是常见的Web安全威胁？", question.Content);
        Assert.Equal("SQL注入|跨站脚本攻击|跨站请求伪造", question.CorrectAnswer);
        Assert.Equal(4, question.Options.Count);
        Assert.Equal("SQL注入、XSS和CSRF都是常见的Web安全威胁。", question.Analysis);
        Assert.Equal(2, question.Tags.Count);
        Assert.Contains("Web安全", question.Tags);
        Assert.Contains("安全威胁", question.Tags);
    }

    [Fact]
    public void Parse_MixedQuestions_ReturnsCorrectResults()
    {
        // Arrange
        var text = @"一、单项选择题（每题1分）
1、SSL协议最早是由(B)提出的。
A、Microsoft
B、Netscape
C、ISO
D、IBM
【解析】SSL协议是由Netscape公司提出的，用于在互联网上提供安全通信。
【标签】SSL、安全通信

二、判断题（每题1分）
1. 平邮包裹的到货周期较长，顾客通常要7 - 15天才能收到购买的商品, 但是提供了网上查询物流进程的服务。（对）
【解析】平邮包裹确实有网上查询物流进程的服务。
【标签】平邮、商品

三、多项选择题（每题2分）
1、以下哪些是常见的Web安全威胁(ABC)？
A、SQL注入
B、跨站脚本攻击
C、跨站请求伪造
D、合法访问
【解析】SQL注入、XSS和CSRF都是常见的Web安全威胁。
【标签】Web安全、安全威胁";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(QuestionType.SingleChoice, result[0].Type);
        Assert.Equal(QuestionType.TrueFalse, result[1].Type);
        Assert.Equal(QuestionType.MultipleChoice, result[2].Type);
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

    [Fact]
    public void Parse_QuestionsWithDifficulty_ReturnsCorrectResults()
    {
        // Arrange
        var text = @"一、单项选择题（每题1分）
1、SSL协议最早是由(B)提出的。
A、Microsoft
B、Netscape
C、ISO
D、IBM
【难度】简单
【解析】SSL协议是由Netscape公司提出的，用于在互联网上提供安全通信。
【标签】SSL、安全通信

2、以下关于HTTPS的说法正确的是(C)？
A、HTTPS不需要证书
B、HTTPS不加密传输
C、HTTPS是安全的HTTP协议
D、HTTPS不能用于网页浏览
【难度】中等
【解析】HTTPS是安全的HTTP协议，需要证书且加密传输。
【标签】HTTPS、安全协议

3、以下哪个不是对称加密算法(D)？
A、DES
B、3DES
C、AES
D、RSA
【难度】困难
【解析】RSA是非对称加密算法，其他都是对称加密算法。
【标签】加密算法、RSA";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Equal(3, result.Count);

        // 验证第一题（简单难度）
        var question1 = result[0];
        Assert.Equal(QuestionType.SingleChoice, question1.Type);
        Assert.Equal(QuestionDifficulty.Easy, question1.Difficulty);
        Assert.Equal("SSL协议最早是由提出的。", question1.Content);

        // 验证第二题（中等难度）
        var question2 = result[1];
        Assert.Equal(QuestionType.SingleChoice, question2.Type);
        Assert.Equal(QuestionDifficulty.Medium, question2.Difficulty);
        Assert.Equal("以下关于HTTPS的说法正确的是？", question2.Content);

        // 验证第三题（困难难度）
        var question3 = result[2];
        Assert.Equal(QuestionType.SingleChoice, question3.Type);
        Assert.Equal(QuestionDifficulty.Hard, question3.Difficulty);
        Assert.Equal("以下哪个不是对称加密算法？", question3.Content);
    }

    [Fact]
    public void Parse_QuestionsWithMixedDifficultyFormats_ReturnsCorrectResults()
    {
        // Arrange
        var text = @"一、判断题（每题1分）
1. 对称加密算法的加密密钥和解密密钥是相同的。（对）
【难度】：简单
【解析】这是对称加密算法的基本特征。
【标签】加密算法、对称加密

2. RSA算法的安全性基于大数分解的困难性。（对）
【难度】:困难
【解析】RSA算法的安全性确实基于大数分解的困难性。
【标签】RSA、密码学

3. HTTPS协议中使用的是对称加密。（错）
【难度】中等
【解析】HTTPS使用混合加密系统，既有对称加密也有非对称加密。
【标签】HTTPS、加密";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Equal(3, result.Count);

        // 验证不同格式的难度标记都能正确解析
        Assert.Equal(QuestionDifficulty.Easy, result[0].Difficulty);
        Assert.Equal(QuestionDifficulty.Hard, result[1].Difficulty);
        Assert.Equal(QuestionDifficulty.Medium, result[2].Difficulty);
    }

    [Fact]
    public void Parse_QuestionsWithoutDifficulty_ReturnsMediumDifficulty()
    {
        // Arrange
        var text = @"一、多选题（每题2分）
1、以下哪些是常见的Web安全威胁(ABC)？
A、SQL注入
B、跨站脚本攻击
C、跨站请求伪造
D、合法访问
【解析】SQL注入、XSS和CSRF都是常见的Web安全威胁。
【标签】Web安全、安全威胁";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Single(result);
        Assert.Equal(QuestionDifficulty.Medium, result[0].Difficulty); // 默认难度应为中等
    }

    [Fact]
    public void Parse_QuestionsWithInvalidDifficulty_ReturnsMediumDifficulty()
    {
        // Arrange
        var text = @"一、单选题（每题1分）
1、以下哪个是Web服务器软件(A)？
A、Apache
B、MySQL
C、Redis
D、MongoDB
【难度】特别难
【解析】Apache是最流行的Web服务器软件之一。
【标签】Web服务器、Apache";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Single(result);
        Assert.Equal(QuestionDifficulty.Medium, result[0].Difficulty); // 无效难度应返回中等
    }


    [Fact]
    public void Parse_QuestionWithSpacesInBrackets_ReturnsCorrectResult()
    {
        // Arrange
        var text = @"一、单项选择题（每题1分）
1、勤劳节俭的现代意义在于（  A   ）。
A、勤劳节俭是促进经济和社会发展的重要手段
B、勤劳是现代市场经济需要的，而节俭则不宜提倡
C、节俭阻碍消费，因而会阻碍市场经济的发展
D、勤劳节俭只有利于节省资源，但与提高生产力无关
【解析】勤劳节俭是促进经济和社会发展的重要手段。
【标签】勤劳节俭、经济发展";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Single(result);
        var question = result[0];
        Assert.Equal(QuestionType.SingleChoice, question.Type);
        Assert.Equal("勤劳节俭的现代意义在于。", question.Content);
        Assert.Equal("勤劳节俭是促进经济和社会发展的重要手段", question.CorrectAnswer);
        Assert.Equal(4, question.Options.Count);
        Assert.Equal("勤劳节俭是促进经济和社会发展的重要手段。", question.Analysis);
        Assert.Equal(2, question.Tags.Count);
        Assert.Contains("勤劳节俭", question.Tags);
        Assert.Contains("经济发展", question.Tags);
    }

    [Fact]
    public void Parse_SingleChoiceWithCodeSnippet_ReturnsCorrectResult()
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
    public void Parse_SingleChoiceWithHTMLTags_ReturnsCorrectResult()
    {
        // Arrange
        var text = @"一、单项选择题（每题1分）
76. 以下标记符中，没有对应的结果标签的是(   B   )。
A、<body>   
B、<br>  
C、<html>   
D、<title>
【解析】HTML中，<br>是单标签，不需要结束标签，而其他选项都需要对应的结束标签（如</body>、</html>、</title>）。
【标签】HTML、标签";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Single(result);
        var question = result[0];
        Assert.Equal(QuestionType.SingleChoice, question.Type);
        Assert.Equal("以下标记符中，没有对应的结果标签的是。", question.Content);
        Assert.Equal("<br>", question.CorrectAnswer);
        Assert.Equal(4, question.Options.Count);
        Assert.Equal("HTML中，<br>是单标签，不需要结束标签，而其他选项都需要对应的结束标签（如</body>、</html>、</title>）。", question.Analysis);
        Assert.Equal(2, question.Tags.Count);
        Assert.Contains("HTML", question.Tags);
        Assert.Contains("标签", question.Tags);
    }

    [Fact]
    public void Parse_SingleChoiceWithFileExtension_ReturnsCorrectResult()
    {
        // Arrange
        var text = @"一、单项选择题（每题1分）
51. 在C# WinForms程序中，创建一个窗体的后缀名为（   A  ）。 
A、.cs
B、.aspx
C、.xml
D、.form
【解析】在C# WinForms应用程序中，窗体文件使用.cs后缀，这是C#源代码文件的标准扩展名。
【标签】WinForms、文件扩展名";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Single(result);
        var question = result[0];
        Assert.Equal(QuestionType.SingleChoice, question.Type);
        Assert.Equal("在C# WinForms程序中，创建一个窗体的后缀名为。", question.Content);
        Assert.Equal(".cs", question.CorrectAnswer);
        Assert.Equal(4, question.Options.Count);
        Assert.Equal("在C# WinForms应用程序中，窗体文件使用.cs后缀，这是C#源代码文件的标准扩展名。", question.Analysis);
        Assert.Equal(2, question.Tags.Count);
        Assert.Contains("WinForms", question.Tags);
        Assert.Contains("文件扩展名", question.Tags);
    }
} 