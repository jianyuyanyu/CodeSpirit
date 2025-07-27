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
        Assert.Equal("SQL注入,跨站脚本攻击,跨站请求伪造", question.CorrectAnswer);
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

    [Fact]
    public void Parse_QuestionsWithBrackets_PreservesBracketsInContent()
    {
        // Arrange
        var text = @"一、单项选择题（每题1分）
1、以下关于HTTP协议（超文本传输协议）的说法，正确的是(B)
A、HTTP是传输层协议
B、HTTP是应用层协议（基于TCP）
C、HTTP只支持GET方法
D、HTTP是加密协议
【解析】HTTP是应用层协议，基于TCP传输。
【标签】HTTP、协议

二、判断题（每题1分）
1. 数据库中的主键（Primary Key）可以为空值。（错）
【解析】主键不能为空值，这是数据库的基本约束。
【标签】数据库、主键

三、多项选择题（每题2分）
1、以下哪些是常见的编程语言（Programming Language）(ABC)？
A、Java（面向对象）
B、Python（解释型）
C、C++（编译型）
D、HTML（标记语言）
【解析】HTML是标记语言，不是编程语言。
【标签】编程语言、分类";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Equal(3, result.Count);
        
        // 验证单选题保留括号
        var singleChoice = result[0];
        Assert.Equal(QuestionType.SingleChoice, singleChoice.Type);
        Assert.Equal("以下关于HTTP协议（超文本传输协议）的说法，正确的是", singleChoice.Content);
        Assert.Equal("HTTP是应用层协议（基于TCP）", singleChoice.CorrectAnswer);
        Assert.Contains("HTTP是应用层协议（基于TCP）", singleChoice.Options);
        
        // 验证判断题保留括号
        var trueFalse = result[1];
        Assert.Equal(QuestionType.TrueFalse, trueFalse.Type);
        Assert.Equal("数据库中的主键（Primary Key）可以为空值。", trueFalse.Content);
        Assert.Equal("False", trueFalse.CorrectAnswer);
        
        // 验证多选题保留括号
        var multipleChoice = result[2];
        Assert.Equal(QuestionType.MultipleChoice, multipleChoice.Type);
        Assert.Equal("以下哪些是常见的编程语言（Programming Language）？", multipleChoice.Content);
        Assert.Contains("Java（面向对象）", multipleChoice.Options);
        Assert.Contains("Python（解释型）", multipleChoice.Options);
        Assert.Contains("C++（编译型）", multipleChoice.Options);
    }

    [Fact]
    public void Parse_QuestionsWithChineseBrackets_ParsesCorrectly()
    {
        // Arrange
        var text = @"一、单项选择题（每题1分）
1、以下关于HTTP协议的说法，正确的是（B）
A、HTTP是传输层协议
B、HTTP是应用层协议
C、HTTP只支持GET方法
D、HTTP是加密协议
【解析】HTTP是应用层协议，基于TCP传输。
【标签】HTTP、协议

二、判断题（每题1分）
1. 数据库中的主键可以为空值。（错）
【解析】主键不能为空值，这是数据库的基本约束。
【标签】数据库、主键

三、多项选择题（每题2分）
1、以下哪些是常见的编程语言（ABC）？
A、Java
B、Python
C、C++
D、HTML
【解析】HTML是标记语言，不是编程语言。
【标签】编程语言、分类";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Equal(3, result.Count);
        
        // 验证单选题
        var singleChoice = result[0];
        Assert.Equal(QuestionType.SingleChoice, singleChoice.Type);
        Assert.Equal("以下关于HTTP协议的说法，正确的是", singleChoice.Content);
        Assert.Equal("HTTP是应用层协议", singleChoice.CorrectAnswer);
        
        // 验证判断题
        var trueFalse = result[1];
        Assert.Equal(QuestionType.TrueFalse, trueFalse.Type);
        Assert.Equal("数据库中的主键可以为空值。", trueFalse.Content);
        Assert.Equal("False", trueFalse.CorrectAnswer);
        
        // 验证多选题
        var multipleChoice = result[2];
        Assert.Equal(QuestionType.MultipleChoice, multipleChoice.Type);
        Assert.Equal("以下哪些是常见的编程语言？", multipleChoice.Content);
        Assert.Contains("Java", multipleChoice.CorrectAnswer);
        Assert.Contains("Python", multipleChoice.CorrectAnswer);
        Assert.Contains("C++", multipleChoice.CorrectAnswer);
    }

    [Fact]
    public void Parse_QuestionsWithDollarSign_ParsesCorrectly()
    {
        // Arrange
        var text = @"一、单项选择题（每题1分）
1、在JavaScript中，以下哪个变量声明是正确的（B）？
A、var $name = ""test"";
B、var $userName = ""admin"";
C、var 123name = ""invalid"";
D、var class = ""reserved"";
【解析】在JavaScript中，变量名可以包含$字符，但不能以数字开头，也不能使用保留字。
【标签】JavaScript、变量声明

二、判断题（每题1分）
1. 在PHP中，所有变量都必须以$符号开头。（对）
【解析】PHP中的变量确实必须以$符号开头，这是PHP的语法规则。
【标签】PHP、变量

三、多项选择题（每题2分）
1、以下哪些是Shell脚本中的有效变量声明（ABC）？
A、$HOME
B、$USER
C、$PATH
D、$123invalid
【解析】Shell脚本中的变量名不能以数字开头。
【标签】Shell、变量";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Equal(3, result.Count);
        
        // 验证单选题中$字符被转义
        var singleChoice = result[0];
        Assert.Equal(QuestionType.SingleChoice, singleChoice.Type);
        Assert.Equal("在JavaScript中，以下哪个变量声明是正确的？", singleChoice.Content);
        Assert.Equal("var $userName = \"admin\";", singleChoice.CorrectAnswer);
        Assert.Contains("var \\$name = \"test\";", singleChoice.Options);
        Assert.Contains("var \\$userName = \"admin\";", singleChoice.Options);
        Assert.Contains("在JavaScript中，变量名可以包含$字符，但不能以数字开头，也不能使用保留字。", singleChoice.Analysis);
        
        // 验证判断题中$字符被转义
        var trueFalse = result[1];
        Assert.Equal(QuestionType.TrueFalse, trueFalse.Type);
        Assert.Equal("在PHP中，所有变量都必须以$符号开头。", trueFalse.Content);
        Assert.Equal("True", trueFalse.CorrectAnswer);
        Assert.Contains("PHP中的变量确实必须以$符号开头，这是PHP的语法规则。", trueFalse.Analysis);
        
        // 验证多选题中$字符被转义
        var multipleChoice = result[2];
        Assert.Equal(QuestionType.MultipleChoice, multipleChoice.Type);
        Assert.Equal("以下哪些是Shell脚本中的有效变量声明？", multipleChoice.Content);
        Assert.Contains("\\$HOME", multipleChoice.Options);
        Assert.Contains("\\$USER", multipleChoice.Options);
        Assert.Contains("\\$PATH", multipleChoice.Options);
        Assert.Contains("\\$123invalid", multipleChoice.Options);
        Assert.Equal("Shell脚本中的变量名不能以数字开头。", multipleChoice.Analysis);
    }

    [Fact]
    public void Parse_QuestionsWithProgrammingSpecialCharacters_ParsesCorrectly()
    {
        // Arrange
        var text = @"一、单项选择题（每题1分）
1、在C#中，以下哪个字符串插值表达式是正确的（A）？
A、$""Hello {name}!""
B、@""Hello {name}!""
C、#""Hello {name}!""
D、%""Hello {name}!""
【解析】C#中使用$符号进行字符串插值。
【标签】C#、字符串插值

2、在正则表达式中，以下哪个模式匹配任意数字（B）？
A、[a-z]
B、\d
C、\w
D、\s
【解析】\d在正则表达式中匹配任意数字字符。
【标签】正则表达式、模式匹配

三、多项选择题（每题2分）
1、以下哪些是有效的SQL查询语句（ABC）？
A、SELECT * FROM users WHERE id = 1;
B、UPDATE users SET name = 'John' WHERE id = 1;
C、DELETE FROM users WHERE age > 65;
D、INVALID SYNTAX users SET name;
【解析】前三个都是有效的SQL语句，最后一个语法错误。
【标签】SQL、数据库查询

二、判断题（每题1分）
1. 在JavaScript中，变量名可以包含$和_字符。（对）
【解析】JavaScript允许变量名包含字母、数字、$和_字符。
【标签】JavaScript、变量命名

2. 在Python中，字符串可以使用单引号'或双引号""来定义。（对）
【解析】Python支持使用单引号或双引号来定义字符串。
【标签】Python、字符串";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Equal(5, result.Count);
        
        // 验证单选题1 - C#字符串插值
        var singleChoice1 = result[0];
        Assert.Equal(QuestionType.SingleChoice, singleChoice1.Type);
        Assert.Equal("在C#中，以下哪个字符串插值表达式是正确的？", singleChoice1.Content);
        Assert.Equal("$\"Hello {name}!\"", singleChoice1.CorrectAnswer);
        Assert.Contains("\\$\"Hello {name}!\"", singleChoice1.Options);
        Assert.Contains("@\"Hello {name}!\"", singleChoice1.Options);
        
        // 验证单选题2 - 正则表达式
        var singleChoice2 = result[1];
        Assert.Equal(QuestionType.SingleChoice, singleChoice2.Type);
        Assert.Equal("在正则表达式中，以下哪个模式匹配任意数字？", singleChoice2.Content);
        Assert.Equal("\\d", singleChoice2.CorrectAnswer);
        Assert.Contains("\\d", singleChoice2.Options);
        Assert.Contains("\\w", singleChoice2.Options);
        Assert.Contains("\\s", singleChoice2.Options);
        
        // 验证多选题 - SQL语句
        var multipleChoice = result[2];
        Assert.Equal(QuestionType.MultipleChoice, multipleChoice.Type);
        Assert.Equal("以下哪些是有效的SQL查询语句？", multipleChoice.Content);
        Assert.Contains("SELECT * FROM users WHERE id = 1;", multipleChoice.Options);
        Assert.Contains("UPDATE users SET name = 'John' WHERE id = 1;", multipleChoice.Options);
        Assert.Contains("DELETE FROM users WHERE age > 65;", multipleChoice.Options);
        Assert.Contains("INVALID SYNTAX users SET name;", multipleChoice.Options);
        
        // 验证判断题1 - JavaScript变量命名
        var trueFalse1 = result[3];
        Assert.Equal(QuestionType.TrueFalse, trueFalse1.Type);
        Assert.Equal("在JavaScript中，变量名可以包含$和_字符。", trueFalse1.Content);
        Assert.Equal("True", trueFalse1.CorrectAnswer);
        
        // 验证判断题2 - Python字符串
        var trueFalse2 = result[4];
        Assert.Equal(QuestionType.TrueFalse, trueFalse2.Type);
        Assert.Equal("在Python中，字符串可以使用单引号'或双引号\"来定义。", trueFalse2.Content);
        Assert.Equal("True", trueFalse2.CorrectAnswer);
    }

    [Fact]
    public void Parse_QuestionsWithSpecialCharactersForAmis_ParsesCorrectly()
    {
        // Arrange - 测试AMIS框架中可能出现问题的特殊字符
        var text = @"一、单项选择题（每题1分）
1、在JavaScript中，以下哪个变量声明包含$字符（B）？
A、var name = ""test"";
B、var $userName = ""admin"";
C、var user_name = ""test"";
D、var userName = ""admin"";
【解析】JavaScript中变量名可以包含$字符，这在jQuery等库中很常见。
【标签】JavaScript、变量命名

2、以下哪个HTML标签是正确的（A）？
A、<div>内容</div>
B、<div>内容<div>
C、div>内容</div>
D、<div内容</div>
【解析】HTML标签必须正确闭合，<div>标签需要对应的</div>结束标签。
【标签】HTML、标签

三、多项选择题（每题2分）
1、以下哪些字符在编程中有特殊含义（ABC）？
A、$ (美元符号)
B、& (与符号)
C、< > (尖括号)
D、# (井号)
【解析】这些字符在不同编程语言和标记语言中都有特殊含义。
【标签】编程、特殊字符

二、判断题（每题1分）
1. 在PHP中，变量$name和变量name是相同的。（错）
【解析】PHP中$name是变量，而name是常量或字符串，两者不同。
【标签】PHP、变量";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Equal(4, result.Count);
        
        // 验证包含$字符的单选题
        var singleChoice1 = result[0];
        Assert.Equal(QuestionType.SingleChoice, singleChoice1.Type);
        Assert.Equal("在JavaScript中，以下哪个变量声明包含$字符？", singleChoice1.Content);
        Assert.Equal("var $userName = \"admin\";", singleChoice1.CorrectAnswer);
        Assert.Contains("var \\$userName = \"admin\";", singleChoice1.Options);
        
        // 验证包含HTML标签的单选题
        var singleChoice2 = result[1];
        Assert.Equal(QuestionType.SingleChoice, singleChoice2.Type);
        Assert.Equal("以下哪个HTML标签是正确的？", singleChoice2.Content);
        Assert.Equal("<div>内容</div>", singleChoice2.CorrectAnswer);
        Assert.Contains("<div>内容</div>", singleChoice2.Options);
        Assert.Contains("<div>内容<div>", singleChoice2.Options);
        
        // 验证包含特殊字符的多选题
        var multipleChoice = result[2];
        Assert.Equal(QuestionType.MultipleChoice, multipleChoice.Type);
        Assert.Equal("以下哪些字符在编程中有特殊含义？", multipleChoice.Content);
        Assert.Contains("\\$ (美元符号)", multipleChoice.Options);
        Assert.Contains("& (与符号)", multipleChoice.Options);
        Assert.Contains("< > (尖括号)", multipleChoice.Options);
        
        // 验证包含$字符的判断题
        var trueFalse = result[3];
        Assert.Equal(QuestionType.TrueFalse, trueFalse.Type);
        Assert.Equal("在PHP中，变量$name和变量name是相同的。", trueFalse.Content);
        Assert.Equal("False", trueFalse.CorrectAnswer);
    }

    [Fact]
    public void Parse_QuestionsWithDollarSignInParser_ProcessesCorrectly()
    {
        // Arrange - 测试解析器层面对$字符的处理
        var text = @"一、单项选择题（每题1分）
1、在JavaScript中，以下哪个变量声明是正确的（B）？
A、var $name = ""test"";
B、var $userName = ""admin"";
C、var 123name = ""invalid"";
D、var class = ""reserved"";
【解析】在JavaScript中，变量名可以包含$字符，但不能以数字开头，也不能使用保留字。
【标签】JavaScript、变量声明

二、判断题（每题1分）
1. 在PHP中，所有变量都必须以$符号开头。（对）
【解析】PHP中的变量确实必须以$符号开头，这是PHP的语法规则。
【标签】PHP、变量

三、多项选择题（每题2分）
1、以下哪些是Shell脚本中的有效变量声明（ABC）？
A、$HOME
B、$USER
C、$PATH
D、$123invalid
【解析】Shell脚本中的变量名不能以数字开头。
【标签】Shell、变量";

        // Act
        var result = _parser.Parse(text);

        // Assert
        Assert.Equal(3, result.Count);
        
        // 验证单选题中$字符被转义
        var singleChoice = result[0];
        Assert.Equal(QuestionType.SingleChoice, singleChoice.Type);
        Assert.Equal("在JavaScript中，以下哪个变量声明是正确的？", singleChoice.Content);
        Assert.Equal("var $userName = \"admin\";", singleChoice.CorrectAnswer);
        Assert.Contains("var \\$name = \"test\";", singleChoice.Options);
        Assert.Contains("var \\$userName = \"admin\";", singleChoice.Options);
        Assert.Equal("在JavaScript中，变量名可以包含$字符，但不能以数字开头，也不能使用保留字。", singleChoice.Analysis);
        
        // 验证判断题中$字符被转义
        var trueFalse = result[1];
        Assert.Equal(QuestionType.TrueFalse, trueFalse.Type);
        Assert.Equal("在PHP中，所有变量都必须以$符号开头。", trueFalse.Content);
        Assert.Equal("True", trueFalse.CorrectAnswer);
        Assert.Equal("PHP中的变量确实必须以$符号开头，这是PHP的语法规则。", trueFalse.Analysis);
        
        // 验证多选题中$字符被转义
        var multipleChoice = result[2];
        Assert.Equal(QuestionType.MultipleChoice, multipleChoice.Type);
        Assert.Equal("以下哪些是Shell脚本中的有效变量声明？", multipleChoice.Content);
        Assert.Contains("\\$HOME", multipleChoice.Options);
        Assert.Contains("\\$USER", multipleChoice.Options);
        Assert.Contains("\\$PATH", multipleChoice.Options);
        Assert.Contains("\\$123invalid", multipleChoice.Options);
        Assert.Equal("Shell脚本中的变量名不能以数字开头。", multipleChoice.Analysis);
    }
} 