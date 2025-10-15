using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Debugger;

namespace mcp_nexus_unit_tests.Debugger
{
    /// <summary>
    /// Tests for CdbCompletionPatterns
    /// </summary>
    public class CdbCompletionPatternsTests
    {
        [Fact]
        public void CdbCompletionPatterns_Class_Exists()
        {
            // This test verifies that the CdbCompletionPatterns class exists and can be instantiated
            Assert.NotNull(typeof(CdbCompletionPatterns));
        }

        #region IsCdbPrompt Tests

        [Fact]
        public void IsCdbPrompt_WithNull_ReturnsFalse()
        {
            // Act
            var result = CdbCompletionPatterns.IsCdbPrompt(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCdbPrompt_WithEmptyString_ReturnsFalse()
        {
            // Act
            var result = CdbCompletionPatterns.IsCdbPrompt("");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCdbPrompt_WithWhitespace_ReturnsFalse()
        {
            // Act
            var result = CdbCompletionPatterns.IsCdbPrompt("   ");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCdbPrompt_WithValidPrompt_ReturnsTrue()
        {
            // Act
            var result = CdbCompletionPatterns.IsCdbPrompt("0:000>");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCdbPrompt_WithValidPromptAndWhitespace_ReturnsTrue()
        {
            // Act
            var result = CdbCompletionPatterns.IsCdbPrompt("  0:000>  ");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCdbPrompt_WithValidPromptAndContent_ReturnsTrue()
        {
            // Act
            var result = CdbCompletionPatterns.IsCdbPrompt("0:000> some command output");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCdbPrompt_WithDifferentProcessId_ReturnsTrue()
        {
            // Act
            var result = CdbCompletionPatterns.IsCdbPrompt("5:123>");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCdbPrompt_WithPromptAtEndOfLine_ReturnsTrue()
        {
            // Act
            var result = CdbCompletionPatterns.IsCdbPrompt("some output\n0:000>");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCdbPrompt_WithPromptAndProcessName_ReturnsTrue()
        {
            // Act
            var result = CdbCompletionPatterns.IsCdbPrompt("0:000:myprocess>");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCdbPrompt_WithInvalidFormat_ReturnsFalse()
        {
            // Act
            var result = CdbCompletionPatterns.IsCdbPrompt("this is just text");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCdbPrompt_WithMalformedPrompt_ReturnsFalse()
        {
            // Act
            var result = CdbCompletionPatterns.IsCdbPrompt("0:00>");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCdbPrompt_WithPartialPrompt_ReturnsFalse()
        {
            // Act
            var result = CdbCompletionPatterns.IsCdbPrompt("0:");

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("0:000>")]
        [InlineData("1:001>")]
        [InlineData("10:123>")]
        [InlineData("  0:000>  ")]
        [InlineData("0:000> command")]
        [InlineData("\n0:000>")]
        [InlineData("0:000:process>")]
        public void IsCdbPrompt_WithVariousValidPrompts_ReturnsTrue(string prompt)
        {
            // Act
            var result = CdbCompletionPatterns.IsCdbPrompt(prompt);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("0:00>")]
        [InlineData("0:0000>")]
        [InlineData(":000>")]
        [InlineData("0:>")]
        [InlineData("random text")]
        public void IsCdbPrompt_WithVariousInvalidPrompts_ReturnsFalse(string prompt)
        {
            // Act
            var result = CdbCompletionPatterns.IsCdbPrompt(prompt);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsUltraSafeCompletion Tests

        [Fact]
        public void IsUltraSafeCompletion_WithNull_ReturnsFalse()
        {
            // Act
            var result = CdbCompletionPatterns.IsUltraSafeCompletion(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsUltraSafeCompletion_WithEmptyString_ReturnsFalse()
        {
            // Act
            var result = CdbCompletionPatterns.IsUltraSafeCompletion("");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsUltraSafeCompletion_WithWhitespace_ReturnsFalse()
        {
            // Act
            var result = CdbCompletionPatterns.IsUltraSafeCompletion("   ");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsUltraSafeCompletion_WithSyntaxError_ReturnsTrue()
        {
            // Act
            var result = CdbCompletionPatterns.IsUltraSafeCompletion("^ Syntax error in command");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUltraSafeCompletion_WithModLoad_ReturnsTrue()
        {
            // Act
            var result = CdbCompletionPatterns.IsUltraSafeCompletion("ModLoad: 00007ff8`12340000 00007ff8`12350000   kernel32.dll");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUltraSafeCompletion_WithModUnload_ReturnsTrue()
        {
            // Act
            var result = CdbCompletionPatterns.IsUltraSafeCompletion("ModUnload: 00007ff8`12340000 00007ff8`12350000   kernel32.dll");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUltraSafeCompletion_WithLeadingWhitespaceAndSyntaxError_ReturnsTrue()
        {
            // Act
            var result = CdbCompletionPatterns.IsUltraSafeCompletion("  ^ Syntax error in command");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUltraSafeCompletion_WithLeadingWhitespaceAndModLoad_ReturnsTrue()
        {
            // Act
            var result = CdbCompletionPatterns.IsUltraSafeCompletion("  ModLoad: 00007ff8`12340000");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUltraSafeCompletion_WithRandomText_ReturnsFalse()
        {
            // Act
            var result = CdbCompletionPatterns.IsUltraSafeCompletion("This is just random text");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsUltraSafeCompletion_WithPatternInMiddle_ReturnsFalse()
        {
            // Act
            var result = CdbCompletionPatterns.IsUltraSafeCompletion("Some text ^ Syntax error in command");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsUltraSafeCompletion_CaseInsensitive_ReturnsTrue()
        {
            // Act
            var result1 = CdbCompletionPatterns.IsUltraSafeCompletion("modload: 00007ff8");
            var result2 = CdbCompletionPatterns.IsUltraSafeCompletion("MODUNLOAD: 00007ff8");

            // Assert
            Assert.True(result1);
            Assert.True(result2);
        }

        [Theory]
        [InlineData("^ Syntax error in command")]
        [InlineData("ModLoad: 00007ff8`12340000")]
        [InlineData("ModUnload: 00007ff8`12340000")]
        [InlineData("  ^ Syntax error in test")]
        [InlineData("  ModLoad:")]
        [InlineData("modload: test")]
        public void IsUltraSafeCompletion_WithVariousValidPatterns_ReturnsTrue(string line)
        {
            // Act
            var result = CdbCompletionPatterns.IsUltraSafeCompletion(line);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("random text")]
        [InlineData("text ^ Syntax error")]
        [InlineData("text ModLoad:")]
        [InlineData("Just some output")]
        [InlineData("0:000>")]
        public void IsUltraSafeCompletion_WithVariousInvalidPatterns_ReturnsFalse(string line)
        {
            // Act
            var result = CdbCompletionPatterns.IsUltraSafeCompletion(line);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCdbPrompt_WithTextThatDoesntMatchAnyPattern_ReturnsFalse()
        {
            // Arrange - A line that will cause the AdditionalPromptPatterns loop to execute but not match
            var line = "Some regular output text without any prompt pattern";

            // Act
            var result = CdbCompletionPatterns.IsCdbPrompt(line);

            // Assert
            Assert.False(result); // Should iterate through AdditionalPromptPatterns but find no match
        }

        [Theory]
        [InlineData("  0:000>  ")]  // PromptSimple with whitespace
        [InlineData("0:000> .echo test")]  // PromptWithText
        [InlineData("output before 0:000> ")]  // PromptEnd at end
        public void IsCdbPrompt_WithAdditionalPatternMatch_ReturnsTrue(string line)
        {
            // Act - These should match AdditionalPromptPatterns
            var result = CdbCompletionPatterns.IsCdbPrompt(line);

            // Assert - Should return true via foreach loop (line 74 TRUE branch)
            Assert.True(result);
        }

        #endregion
    }
}
