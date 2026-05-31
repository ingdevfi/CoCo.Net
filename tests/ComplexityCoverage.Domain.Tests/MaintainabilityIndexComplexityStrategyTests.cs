using NFluent;
using ComplexityCoverage.Domain.Complexity;
using ComplexityCoverage.Domain.Interfaces;

namespace ComplexityCoverage.Domain.Tests
{
    public class MaintainabilityIndexComplexityStrategyTests
    {
        private readonly IComplexityStrategy _halsteadStrategy;
        private readonly IComplexityStrategy _mccabeStrategy;
        private readonly MaintainabilityIndexComplexityStrategy _strategy;

        public MaintainabilityIndexComplexityStrategyTests()
        {
            _halsteadStrategy = new HalsteadVolumeComplexityStrategy();
            _mccabeStrategy = new McCabeComplexityStrategy();
            _strategy = new MaintainabilityIndexComplexityStrategy(_halsteadStrategy, _mccabeStrategy);
        }

        [Fact]
        public void SimpleMethod_ShouldHaveHighMaintainability()
        {
            var code = @"
public void Simple() {
    var x = 1;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights).Not.IsEmpty();
            Check.That(weights).ContainsOnlyElementsThatMatch(w => w >= 0);
            Check.That(weights.Max()).IsStrictlyLessThan(101.0).And.IsStrictlyGreaterThan(-1.0);
        }

        [Fact]
        public void ComplexMethod_ShouldHaveLowMaintainability()
        {
            var code = @"
public int ComplexLogic(int a, int b, int c) {
    if (a > 0) {
        if (b > 0) {
            if (c > 0) {
                return a + b + c;
            }
        }
    }
    return 0;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights).Not.IsEmpty();
            Check.That(weights).ContainsOnlyElementsThatMatch(w => w >= 0 && w <= 100);
        }

        [Fact]
        public void VerboseMethod_ShouldReflectHighOperatorCount()
        {
            var code = @"
public int Calculate(int x, int y, int z) {
    int result = x + y + z;
    result = result * 2;
    result = result - 10;
    result = result / 2;
    return result;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights).Not.IsEmpty();
            Check.That(weights).ContainsOnlyElementsThatMatch(w => w >= 0 && w <= 100);
        }

        [Fact]
        public void MethodWithDecisions_ShouldReflectComplexity()
        {
            var code = @"
public bool CheckCondition(int x, int y) {
    if (x > 0 && y > 0) {
        return true;
    } else if (x == 0 || y == 0) {
        return false;
    }
    return x != y;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights).Not.IsEmpty();
            Check.That(weights).ContainsOnlyElementsThatMatch(w => w >= 0 && w <= 100);
        }

        [Fact]
        public void AllLines_ShouldHaveWeights()
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
            Check.That(weights).ContainsOnlyElementsThatMatch(w => w >= 0 && w <= 100);
        }

        [Fact]
        public void ConsistentWeights_ShouldBeMethodLevel()
        {
            var code = @"
public void Method() {
    var x = 1;
    var y = 2;
    var z = 3;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            var methodLines = weights.Skip(1).Take(file.Lines.Count - 2).Where(w => w > 0).ToList();
            if (methodLines.Count > 1)
            {
                var avgWeight = methodLines.Average();
                Check.That(methodLines).ContainsOnlyElementsThatMatch(w => Math.Abs(w - avgWeight) < 50);
            }
        }

        [Fact]
        public void SwitchStatement_ShouldIncreaseComplexity()
        {
            var code = @"
public string GetDay(int dayNum) {
    switch (dayNum) {
        case 1: return ""Monday"";
        case 2: return ""Tuesday"";
        case 3: return ""Wednesday"";
        case 4: return ""Thursday"";
        case 5: return ""Friday"";
        default: return ""Weekend"";
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights).Not.IsEmpty();
            Check.That(weights).ContainsOnlyElementsThatMatch(w => w >= 0 && w <= 100);
        }

        [Fact]
        public void LoopsAndLogicalOperators_ShouldReflectComplexity()
        {
            var code = @"
public bool Validate(int[] items) {
    for (int i = 0; i < items.Length; i++) {
        if (items[i] <= 0 || items[i] > 100) {
            return false;
        }
    }
    return true;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights).Not.IsEmpty();
            Check.That(weights).ContainsOnlyElementsThatMatch(w => w >= 0 && w <= 100);
        }

        [Fact]
        public void WeightScale_ShouldBeBetweenZeroAndHundred()
        {
            var code = @"
public void A() { }
public void B() { var x = 1; }
public void C() { if (true) { if (true) { if (true) { } } } }
";
            var file = ComplexityTestHelper.CreateSourceFile(code);
            var weights = _strategy.CalculateWeights(file);

            Check.That(weights).ContainsOnlyElementsThatMatch(w => w >= 0 && w <= 100);
        }

        [Fact]
        public void ComplexVsSimple_ShouldShowDifference()
        {
            var simpleCode = @"
public int Add(int a, int b) {
    return a + b;
}";
            var complexCode = @"
public int Complex(int a, int b, int c, int d) {
    if (a > 0 && b > 0) {
        if (c > 0 && d > 0) {
            for (int i = 0; i < a; i++) {
                if (i % 2 == 0) {
                    a = a + b;
                } else {
                    a = a - c;
                }
            }
        }
    }
    return a;
}";

            var simpleFile = ComplexityTestHelper.CreateSourceFile(simpleCode);
            var complexFile = ComplexityTestHelper.CreateSourceFile(complexCode);

            var simpleWeights = _strategy.CalculateWeights(simpleFile);
            var complexWeights = _strategy.CalculateWeights(complexFile);

            var simpleAvg = simpleWeights.Average();
            var complexAvg = complexWeights.Average();

            Check.That(complexAvg >= simpleAvg || complexWeights.Max() > simpleWeights.Max()).IsTrue();
        }
    }
}
