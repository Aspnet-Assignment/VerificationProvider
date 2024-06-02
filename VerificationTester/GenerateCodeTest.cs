using Microsoft.Extensions.Logging;
using Moq;
using VerificationProvider; 

namespace VerificationTester;

public class GenerateCodeTest
{
    
   
        private readonly CodeGenerator _codeGenerator;
        private readonly Mock<ILogger<CodeGenerator>> _mockLogger;

        public GenerateCodeTest()
        {
            _mockLogger = new Mock<ILogger<CodeGenerator>>();
            _codeGenerator = new CodeGenerator(_mockLogger.Object);
        }

        [Fact]
        public void GeneratedCode_ShouldReturnSixDigitCode()
        {
            
            var result = _codeGenerator.GeneratedCode();

            
            Assert.False(string.IsNullOrEmpty(result), "Result should not be null or empty");
            Assert.True(result.Length == 6, "Result should be a 6-digit code");
            Assert.True(int.TryParse(result, out _), "Result should be a valid integer");
        }

        [Fact]
        public void GeneratedCode_ShouldReturnNull_WhenExceptionIsThrown()
        {
            
            var mockRandom = new Mock<Random>();
            mockRandom.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Throws(new Exception("Test exception"));

            var codeGenerator = new CodeGeneratorWithMockRandom(_mockLogger.Object, mockRandom.Object);

            
            var result = codeGenerator.GeneratedCode();

            
            Assert.Null(result);
        }
    

    public class CodeGeneratorWithMockRandom : CodeGenerator
    {
        private readonly Random _random;

        public CodeGeneratorWithMockRandom(ILogger<CodeGenerator> logger, Random random) : base(logger)
        {
            _random = random;
        }

        protected override Random GetRandomInstance()
        {
            return _random;
        }
    }

    public class CodeGenerator
    {
        private readonly ILogger<CodeGenerator> _logger;

        public CodeGenerator(ILogger<CodeGenerator> logger)
        {
            _logger = logger;
        }

        public virtual string GeneratedCode()
        {
            try
            {
                var rnd = GetRandomInstance();
                var code = rnd.Next(100000, 999999);
                return code.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR : GeneratedCode  :: {ex.Message}");
                return null!;
            }
        }

        protected virtual Random GetRandomInstance()
        {
            return new Random();
        }
    }
}

