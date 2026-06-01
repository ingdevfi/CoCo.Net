using NFluent;
using ComplexityCoverage.Domain.Complexity;
using ComplexityCoverage.Domain.Interfaces;

namespace ComplexityCoverage.Domain.Tests
{
    /// <summary>
    /// Documentation example tests — validates the exact weights shown in strategy docs.
    /// Each test corresponds to a code example in doc/strategies/*.md
    /// </summary>
    public class StrategyDocumentationExampleTests
    {
        private readonly IComplexityStrategy _mccabe = new McCabeComplexityStrategy();
        private readonly IComplexityStrategy _nesting = new NestingComplexityStrategy();
        private readonly IComplexityStrategy _halstead = new HalsteadVolumeComplexityStrategy();
        private readonly IComplexityStrategy _mi = new MaintainabilityIndexComplexityStrategy(
            new HalsteadVolumeComplexityStrategy(), new McCabeComplexityStrategy());

        #region Example code snippets

        private const string SimpleIf = @"
public void Example() {
    if (x > 0) {
        var a = 1;
    }
}";

        private const string IfElse = @"
public void Example() {
    if (x > 0) {
        var a = 1;
    } else {
        var b = 2;
    }
}";

        private const string NestedIfElse = @"
public void Example() {
    if (x > 0) {
        if (y > 0) {
            var a = 1;
        } else {
            var b = 2;
        }
    } else {
        var c = 3;
    }
}";

        private const string MultipleConditions = @"
public void Example() {
    if (x > 0 && y > 0 || z == 1) {
        var a = 1;
    }
}";

        private const string Ternary = @"
public void Example() {
    var result = x > 0 ? 1 : 0;
}";

        private const string Switch = @"
public void Example() {
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

        private const string MultipleReturns = @"
public int Example() {
    if (x > 10) {
        return 1;
    }
    if (x > 5) {
        return 2;
    }
    return 0;
}";

        private const string TryCatchFinally = @"
public void Example() {
    try {
        var a = DoSomething();
    } catch (Exception ex) {
        Log(ex);
    } finally {
        Cleanup();
    }
}";

        #endregion

        #region McCabe tests

        [Fact]
        public void McCabe_SimpleIf()
        {
            var weights = _mccabe.CalculateWeights(ComplexityTestHelper.CreateSourceFile(SimpleIf));
            // 1 (base) + 1 (if) = 2
            Check.That(weights[3]).IsEqualTo(2);
        }

        [Fact]
        public void McCabe_IfElse()
        {
            var weights = _mccabe.CalculateWeights(ComplexityTestHelper.CreateSourceFile(IfElse));
            // 1 (base) + 1 (if) = 2 — else does not add a decision point
            Check.That(weights[3]).IsEqualTo(2);
            Check.That(weights[5]).IsEqualTo(2);
        }

        [Fact]
        public void McCabe_NestedIfElse()
        {
            var weights = _mccabe.CalculateWeights(ComplexityTestHelper.CreateSourceFile(NestedIfElse));
            // 1 (base) + 1 (outer if) + 1 (inner if) = 3
            Check.That(weights[4]).IsEqualTo(3);
        }

        [Fact]
        public void McCabe_MultipleConditions()
        {
            var weights = _mccabe.CalculateWeights(ComplexityTestHelper.CreateSourceFile(MultipleConditions));
            // 1 (base) + 1 (if) + 1 (&&) + 1 (||) = 4
            Check.That(weights[3]).IsEqualTo(4);
        }

        [Fact]
        public void McCabe_Ternary()
        {
            var weights = _mccabe.CalculateWeights(ComplexityTestHelper.CreateSourceFile(Ternary));
            // 1 (base) + 1 (ternary) = 2
            Check.That(weights[2]).IsEqualTo(2);
        }

        [Fact]
        public void McCabe_Switch()
        {
            var weights = _mccabe.CalculateWeights(ComplexityTestHelper.CreateSourceFile(Switch));
            // 1 (base) + 3 (case/default sections) = 4
            Check.That(weights[5]).IsEqualTo(4);
        }

        [Fact]
        public void McCabe_MultipleReturns()
        {
            var weights = _mccabe.CalculateWeights(ComplexityTestHelper.CreateSourceFile(MultipleReturns));
            Check.That(weights[3]).IsEqualTo(4);
        }

        [Fact]
        public void McCabe_TryCatchFinally()
        {
            var weights = _mccabe.CalculateWeights(ComplexityTestHelper.CreateSourceFile(TryCatchFinally));
            // 1 (base) — try/catch/finally are not decision points in McCabe
            Check.That(weights[3]).IsEqualTo(1);
        }

        #endregion

        #region Nesting tests

        [Fact]
        public void Nesting_SimpleIf()
        {
            var weights = _nesting.CalculateWeights(ComplexityTestHelper.CreateSourceFile(SimpleIf));
            // Line inside if: 1 (base) + 1 (if depth) = 2
            Check.That(weights[3]).IsEqualTo(2);
        }

        [Fact]
        public void Nesting_IfElse()
        {
            var weights = _nesting.CalculateWeights(ComplexityTestHelper.CreateSourceFile(IfElse));
            // Both branches at depth 1: 1 + 1 = 2
            Check.That(weights[3]).IsEqualTo(2);
            Check.That(weights[5]).IsEqualTo(2);
        }

        [Fact]
        public void Nesting_NestedIfElse()
        {
            var weights = _nesting.CalculateWeights(ComplexityTestHelper.CreateSourceFile(NestedIfElse));
            // Inner body at depth 2: 1 + 2 = 3
            Check.That(weights[4]).IsEqualTo(3);
        }

        [Fact]
        public void Nesting_MultipleConditions()
        {
            var weights = _nesting.CalculateWeights(ComplexityTestHelper.CreateSourceFile(MultipleConditions));
            // Inside if with && and ||: 1 (base) + 1 (if) + 2 (&&, ||) = 4
            Check.That(weights[3]).IsEqualTo(4);
        }

        [Fact]
        public void Nesting_Ternary()
        {
            var weights = _nesting.CalculateWeights(ComplexityTestHelper.CreateSourceFile(Ternary));
            // Ternary adds 1 level: 1 + 1 = 2
            Check.That(weights[2]).IsEqualTo(2);
        }

        [Fact]
        public void Nesting_Switch()
        {
            var weights = _nesting.CalculateWeights(ComplexityTestHelper.CreateSourceFile(Switch));
            // Inside switch case section (3 sections): 1 + 3 = 4
            Check.That(weights[5]).IsEqualTo(4);
        }

        [Fact]
        public void Nesting_MultipleReturns()
        {
            var weights = _nesting.CalculateWeights(ComplexityTestHelper.CreateSourceFile(MultipleReturns));
            // Line inside first if: 1 + 1 = 2
            Check.That(weights[3]).IsEqualTo(2);
        }

        [Fact]
        public void Nesting_TryCatchFinally()
        {
            var weights = _nesting.CalculateWeights(ComplexityTestHelper.CreateSourceFile(TryCatchFinally));
            // try/catch/finally don't add nesting depth in this implementation
            Check.That(weights[3]).IsEqualTo(1);
        }

        #endregion

        #region Halstead tests

        [Fact]
        public void Halstead_SimpleIf()
        {
            var weights = _halstead.CalculateWeights(ComplexityTestHelper.CreateSourceFile(SimpleIf));
            // if line has operators and operands → positive volume
            Check.That(weights[2]).IsStrictlyGreaterThan(0);
        }

        [Fact]
        public void Halstead_Ternary()
        {
            var weights = _halstead.CalculateWeights(ComplexityTestHelper.CreateSourceFile(Ternary));
            // Ternary line: var, result, =, x, >, 0, ?, 1, :, 0, ; — rich vocabulary
            Check.That(weights[2]).IsStrictlyGreaterThan(10);
        }

        [Fact]
        public void Halstead_TryCatchFinally()
        {
            var weights = _halstead.CalculateWeights(ComplexityTestHelper.CreateSourceFile(TryCatchFinally));
            // Method call line has operators/operands
            Check.That(weights[3]).IsStrictlyGreaterThan(0);
        }

        #endregion

        #region MI tests

        [Fact]
        public void MI_SimpleIf_WeightBetween0And100()
        {
            var weights = _mi.CalculateWeights(ComplexityTestHelper.CreateSourceFile(SimpleIf));
            Check.That(weights).ContainsOnlyElementsThatMatch(w => w >= 0 && w <= 100);
        }

        [Fact]
        public void MI_TryCatchFinally_WeightBetween0And100()
        {
            var weights = _mi.CalculateWeights(ComplexityTestHelper.CreateSourceFile(TryCatchFinally));
            Check.That(weights).ContainsOnlyElementsThatMatch(w => w >= 0 && w <= 100);
        }

        [Fact]
        public void MI_ComplexMethodHigherThanSimple()
        {
            var simpleWeights = _mi.CalculateWeights(ComplexityTestHelper.CreateSourceFile(SimpleIf));
            var complexWeights = _mi.CalculateWeights(ComplexityTestHelper.CreateSourceFile(NestedIfElse));

            // Complex code should have higher average non-zero weight
            var simpleNonZero = simpleWeights.Where(w => w > 0).DefaultIfEmpty(0).Average();
            var complexNonZero = complexWeights.Where(w => w > 0).DefaultIfEmpty(0).Average();

            Check.That(complexNonZero).IsStrictlyGreaterThan(simpleNonZero);
        }

        #endregion
    }
}
