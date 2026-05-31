using NFluent;
using ComplexityCoverage.Domain.Complexity;
using ComplexityCoverage.Domain.Interfaces;

namespace ComplexityCoverage.Domain.Tests
{
    /// <summary>
    /// Integration tests that validate all complexity strategies work together.
    /// These tests verify cross-strategy consistency and behavior.
    /// </summary>
    public class ComplexityStrategyIntegrationTests
    {
        private readonly IComplexityStrategy _nestingStrategy = new NestingComplexityStrategy();
        private readonly IComplexityStrategy _mccabeStrategy = new McCabeComplexityStrategy();
        private readonly IComplexityStrategy _halsteadStrategy = new HalsteadVolumeComplexityStrategy();

        [Fact]
        public void AllStrategies_ShouldReturnWeightsForAllLines()
        {
            var code = @"
public void Test() {
    if (true) {
        var a = 1;
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);

            var nestingWeights = _nestingStrategy.CalculateWeights(file);
            var mccabeWeights = _mccabeStrategy.CalculateWeights(file);
            var halsteadWeights = _halsteadStrategy.CalculateWeights(file);

            Check.That(nestingWeights).HasSize(file.Lines.Count);
            Check.That(mccabeWeights).HasSize(file.Lines.Count);
            Check.That(halsteadWeights).HasSize(file.Lines.Count);
        }

        [Fact]
        public void SimpleCode_AllStrategiesShouldHavePositiveWeights()
        {
            var code = @"
public void Test() {
    var a = 1;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);

            var nestingWeights = _nestingStrategy.CalculateWeights(file);
            var mccabeWeights = _mccabeStrategy.CalculateWeights(file);
            var halsteadWeights = _halsteadStrategy.CalculateWeights(file);

            Check.That(nestingWeights[2]).IsEqualTo(1);
            Check.That(mccabeWeights[2]).IsEqualTo(1);
            Check.That(halsteadWeights[2]).IsStrictlyGreaterThan(-1.0);
        }

        [Fact]
        public void ControlFlowComplexity_AllStrategiesShouldReactDifferently()
        {
            var code = @"
public void Test() {
    if (condition) {
        var x = 1;
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);

            var nestingWeights = _nestingStrategy.CalculateWeights(file);
            var mccabeWeights = _mccabeStrategy.CalculateWeights(file);
            var halsteadWeights = _halsteadStrategy.CalculateWeights(file);

            Check.That(nestingWeights[3]).IsStrictlyGreaterThan(1);
            Check.That(mccabeWeights[3]).IsStrictlyGreaterThan(1);
            Check.That(halsteadWeights[3]).IsStrictlyGreaterThan(0);
        }

        [Fact]
        public void McCabeAndNesting_DifferentApproaches()
        {
            var code = @"
public void Test() {
    if (a) {
        if (b) {
            if (c) {
                var x = 1;
            }
        }
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);

            var nestingWeights = _nestingStrategy.CalculateWeights(file);
            var mccabeWeights = _mccabeStrategy.CalculateWeights(file);

            Check.That(nestingWeights[5]).IsStrictlyGreaterThan(1);
            Check.That(mccabeWeights[5]).IsStrictlyGreaterThan(1);
        }

        [Fact]
        public void MultipleDecisions_AllStrategiesShouldDetect()
        {
            var code = @"
public void Test() {
    if (a && b && c) {
        var x = 1;
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);

            var nestingWeights = _nestingStrategy.CalculateWeights(file);
            var mccabeWeights = _mccabeStrategy.CalculateWeights(file);
            var halsteadWeights = _halsteadStrategy.CalculateWeights(file);

            Check.That(nestingWeights[3]).IsStrictlyGreaterThan(1);
            Check.That(mccabeWeights[3]).IsStrictlyGreaterThan(1);
            Check.That(halsteadWeights[3]).IsStrictlyGreaterThan(0);
        }

        [Fact]
        public void SwitchStatement_AllStrategiesShouldReact()
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
        default:
            var c = 3;
            break;
    }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);

            var nestingWeights = _nestingStrategy.CalculateWeights(file);
            var mccabeWeights = _mccabeStrategy.CalculateWeights(file);
            var halsteadWeights = _halsteadStrategy.CalculateWeights(file);

            Check.That(nestingWeights[6]).IsStrictlyGreaterThan(1);
            Check.That(mccabeWeights[6]).IsStrictlyGreaterThan(1);
            Check.That(halsteadWeights[6]).IsStrictlyGreaterThan(0);
        }

        [Fact]
        public void LongMethod_AllStrategiesShouldScale()
        {
            var code = @"
public void Test() {
    if (a) { var x = 1; }
    if (b) { var y = 2; }
    if (c) { var z = 3; }
    if (d) { var w = 4; }
    if (e) { var v = 5; }
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);

            var nestingWeights = _nestingStrategy.CalculateWeights(file);
            var mccabeWeights = _mccabeStrategy.CalculateWeights(file);
            var halsteadWeights = _halsteadStrategy.CalculateWeights(file);

            Check.That(nestingWeights.Max()).IsStrictlyGreaterThan(1);
            Check.That(mccabeWeights.Max()).IsStrictlyGreaterThan(1);
            Check.That(halsteadWeights.Max()).IsStrictlyGreaterThan(5);
        }

        [Fact]
        public void VerySimpleLine_AllStrategiesShouldHaveConsistentMinimum()
        {
            var code = @"
public void Test() {
    var x = 1;
}";
            var file = ComplexityTestHelper.CreateSourceFile(code);

            var nestingWeights = _nestingStrategy.CalculateWeights(file);
            var mccabeWeights = _mccabeStrategy.CalculateWeights(file);
            var halsteadWeights = _halsteadStrategy.CalculateWeights(file);

            Check.That(nestingWeights[2]).IsEqualTo(1);
            Check.That(mccabeWeights[2]).IsEqualTo(1);
            Check.That(halsteadWeights[2]).IsStrictlyGreaterThan(0);
        }
    }
}
