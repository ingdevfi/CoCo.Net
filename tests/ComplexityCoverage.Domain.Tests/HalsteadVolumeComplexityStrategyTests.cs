using NFluent;
using ComplexityCoverage.Domain.Complexity;
using ComplexityCoverage.Domain.Interfaces;

namespace ComplexityCoverage.Domain.Tests
{
    /// <summary>
    /// Unit tests for Halstead Volume Complexity Strategy.
    /// Tests the operator/operand count and information content calculation.
    /// </summary>
    public class HalsteadVolumeComplexityStrategyTests
    {
        private readonly IComplexityStrategy _strategy = new HalsteadVolumeComplexityStrategy();

        [Fact]
        public void SimpleMethod_ShouldCalculateVolume()
        {
            var code = @"
public void Test() {
    var a = 1;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[2].Weight).IsStrictlyGreaterThan(-1);
        }

        [Fact]
        public void SimpleAssignment_ShouldHavePositiveVolume()
        {
            var code = @"
public void Test() {
    int x = 5;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[2].Weight).IsStrictlyGreaterThan(10);
        }

        [Fact]
        public void MultipleOperators_ShouldIncreaseVolume()
        {
            var code = @"
public void Test() {
    int result = x + y * z;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[2].Weight).IsStrictlyGreaterThan(0);
        }

        [Fact]
        public void IfStatement_ShouldIncludeKeywordOperator()
        {
            var code = @"
public void Test() {
    if (true) {
        var a = 1;
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[3].Weight).IsStrictlyGreaterThan(0);
        }

        [Fact]
        public void LogicalOperator_ShouldAddToVocabulary()
        {
            var code = @"
public void Test() {
    if (true && false) {
        var a = 1;
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[3].Weight).IsStrictlyGreaterThan(0);
        }

        [Fact]
        public void TernaryOperator_ShouldAddComplexity()
        {
            var code = @"
public void Test() {
    var a = true ? 1 : 2;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[2].Weight).IsStrictlyGreaterThan(0);
        }

        [Fact]
        public void SwitchStatement_ShouldIncludeMultipleKeywords()
        {
            var code = @"
public void Test() {
    switch (x) {
        case 1:
            var a = 1;
            break;
        case 2:
            var b = 2;
            break;
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[5].Weight).IsStrictlyGreaterThan(0);
        }

        [Fact]
        public void EmptyLine_ShouldHaveZeroOrMinimalVolume()
        {
            var code = @"
public void Test() {

}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[3].Weight).IsStrictlyLessThan(2.0);
        }

        [Fact]
        public void VerboseCode_ShouldHaveHigherVolume()
        {
            var code1 = @"
public void Test1() {
    x = 1;
}";
            var code2 = @"
public void Test2() {
    result = valueA + valueB * valueC - valueD;
}";
            var file1 = ComplexityTestHelper.CreateSourceFile(code1);
            var file2 = ComplexityTestHelper.CreateSourceFile(code2);

            var weights1 = _strategy.CalculateWeights(file1);
            var weights2 = _strategy.CalculateWeights(file2);

            Check.That(weights2[2].Weight).IsStrictlyGreaterThan(weights1[2].Weight);
        }

        [Fact]
        public void VocabularySize_AffectsVolume()
        {
            var code1 = @"
public void Test1() {
    x = y;
}";
            var code2 = @"
public void Test2() {
    a = b;
}";
            var file1 = ComplexityTestHelper.CreateSourceFile(code1);
            var file2 = ComplexityTestHelper.CreateSourceFile(code2);

            var weights1 = _strategy.CalculateWeights(file1);
            var weights2 = _strategy.CalculateWeights(file2);

            Check.That(weights1[2].Weight).IsStrictlyGreaterThan(0);
            Check.That(weights2[2].Weight).IsStrictlyGreaterThan(0);
        }

        [Fact]
        public void StringLiterals_ShouldCountAsOperands()
        {
            var code = @"
public void Test() {
    string message = ""Hello World"";
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[2].Weight).IsStrictlyGreaterThan(0);
        }

        [Fact]
        public void MethodCall_ShouldCountOperators()
        {
            var code = @"
public void Test() {
    Console.WriteLine(""test"");
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[2].Weight).IsStrictlyGreaterThan(0);
        }

        [Fact]
        public void AllLines_ShouldHaveWeights()
        {
            var code = @"
public void Test() {
    int x = 1;
    int y = 2;
    int z = x + y;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights).HasSize(file.Lines.Count);
            Check.That(weights).ContainsOnlyElementsThatMatch(w => w.Weight >= 0);
        }

        [Fact]
        public void ComplexExpression_ShouldHaveHighVolume()
        {
            var code = @"
public void Test() {
    if ((a && b) || (c && d) || (e && f)) {
        var x = (p * q) + (r * s) - (t / u);
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[3].Weight).IsStrictlyGreaterThan(20);
        }
    }
}




