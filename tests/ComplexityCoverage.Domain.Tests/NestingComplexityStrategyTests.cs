using NFluent;
using ComplexityCoverage.Domain.Complexity;
using ComplexityCoverage.Domain.Interfaces;

namespace ComplexityCoverage.Domain.Tests
{
    /// <summary>
    /// Unit tests for Nesting Complexity Strategy.
    /// Tests the line-level nesting depth and readability impact calculation.
    /// </summary>
    public class NestingComplexityStrategyTests
    {
        private readonly IComplexityStrategy _strategy = new NestingComplexityStrategy();

        [Fact]
        public void SimpleMethod_ShouldHaveWeightOne()
        {
            var code = @"
public void Test() {
    var a = 1;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[2].Weight).IsEqualTo(1);
        }

        [Fact]
        public void SimpleIf_ShouldIncreaseWeight()
        {
            var code = @"
public void Test() {
    if (true) {
        var a = 1;
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[3].Weight).IsEqualTo(2);
        }

        [Fact]
        public void SimpleIfElse_ShouldHaveSameWeightInBothBranches()
        {
            var code = @"
public void Test() {
    if (true) {
        var a = 1;
    } else {
        var b = 2;
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[3].Weight).IsEqualTo(2);
            Check.That(weights[5].Weight).IsEqualTo(2);
        }

        [Fact]
        public void NestedIf_ShouldIncreaseDueToDepth()
        {
            var code = @"
public void Test() {
    if (a) {
        if (b) {
            var c = 1;
        }
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[4].Weight).IsEqualTo(3);
        }

        [Fact]
        public void TripleNesting_ShouldIncreaseLinearly()
        {
            var code = @"
public void Test() {
    if (a) {
        if (b) {
            if (c) {
                var d = 1;
            }
        }
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[5].Weight).IsEqualTo(4);
        }

        [Fact]
        public void LogicalOperators_ShouldAddToNesting()
        {
            var code = @"
public void Test() {
    if (true && true) {
        var a = 1;
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[3].Weight).IsEqualTo(3);
        }

        [Fact]
        public void TernaryOperator_ShouldIncreaseWeight()
        {
            var code = @"
public void Test() {
    var a = true ? 1 : 2;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[2].Weight).IsEqualTo(2);
        }

        [Fact]
        public void SwitchStatement_ShouldAddComplexity()
        {
            var code = @"
public void Test() {
    int x = 1;
    switch (x) {
        case 1:
            var a = 1;
            break;
        case 2:
            var b = 2;
            break;
        default:
            var c = 3;
            break;
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[6].Weight).IsEqualTo(4);
        }

        [Fact]
        public void NestedLoops_ShouldIncreaseDueToNesting()
        {
            var code = @"
public void Test() {
    for (int i = 0; i < 10; i++) {
        for (int j = 0; j < 10; j++) {
            var a = 1;
        }
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[4].Weight).IsEqualTo(3);
        }

        [Fact]
        public void OutsideNesting_ShouldHaveWeightOne()
        {
            var code = @"
public void Test() {
    if (true) {
        var a = 1;
    }
    var b = 2;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[6].Weight).IsEqualTo(1);
        }

        [Fact]
        public void ReadabilityFocus_VersusPathCount()
        {
            var code = @"
public void Test() {
    if (a && b) {
        var x = 1;
    } else if (c || d) {
        var y = 2;
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[3].Weight).IsStrictlyGreaterThan(1);
            Check.That(weights[5].Weight).IsStrictlyGreaterThan(1);
        }
    }
}



