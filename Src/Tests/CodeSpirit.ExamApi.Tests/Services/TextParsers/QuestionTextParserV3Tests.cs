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
}