using NFluent;
using ComplexityCoverage.Domain.Complexity;
using ComplexityCoverage.Domain.Interfaces;

namespace ComplexityCoverage.Domain.Tests
{
    /// <summary>
    /// Unit tests for McCabe Complexity Strategy.
    /// Tests the method-level cyclomatic complexity calculation.
    /// </summary>
    public class McCabeComplexityStrategyTests
    {
        private readonly IComplexityStrategy _strategy = new McCabeComplexityStrategy();

        [Fact]
        public void SimpleMethod_ShouldHaveWeightOne()
        {
            var code = @"
public void Test() {
    var a = 1;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[2]).IsEqualTo(1);
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

            Check.That(weights[3]).IsEqualTo(2);
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

            Check.That(weights[3]).IsEqualTo(2);
            Check.That(weights[5]).IsEqualTo(2);
        }

        [Fact]
        public void NestedIf_ShouldAccumulateComplexity()
        {
            var code = @"
public void Test() {
    var a = true;
    var b = true;
    if (a) {
        if (b) {
            var c = 1;
        } else {
            var d = 2;
        }
    } else {
        var e = 3;
    }
    var f = 4;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[7]).IsStrictlyGreaterThan(2);
        }

        [Fact]
        public void LogicalOperators_ShouldIncreaseWeight()
        {
            var code = @"
public void Test() {
    if (true && true) {
        var a = 1;
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[3]).IsEqualTo(3);
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

            Check.That(weights[2]).IsEqualTo(2);
        }

        [Fact]
        public void SwitchStatement_ShouldCountCases()
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

            Check.That(weights[6]).IsEqualTo(4);
        }

        [Fact]
        public void MultipleLoops_ShouldAccumulateComplexity()
        {
            var code = @"
public void Test() {
    for (int i = 0; i < 10; i++) {
        var a = 1;
    }
    while (true) {
        var b = 2;
        break;
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights[3]).IsStrictlyGreaterThan(1);
            Check.That(weights[6]).IsStrictlyGreaterThan(1);
        }

        [Fact]
        public void AllLines_ShouldHaveConsistentWeights()
        {
            var code = @"
public void Test() {
    var a = 1;
    if (true) {
        var b = 2;
    }
    var c = 3;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights).HasSize(file.Lines.Count);
            Check.That(weights).ContainsOnlyElementsThatMatch(w => w >= 0);
            Check.That(weights.Max()).IsStrictlyGreaterThan(0);
        }
    }
}
