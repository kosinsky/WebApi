using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Query.Validators;
using Microsoft.AspNet.OData.Test.Common;
using Microsoft.AspNet.OData.Test.Query.Expressions;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.AspNet.OData.Test.Query.Validators
{
    public class ComputeQueryValidatorTest
    {
        private MyComputeValidator _validator;
        private ODataValidationSettings _settings = new ODataValidationSettings();
        private ODataQueryContext _context;
        private ODataQueryContext _productContext;

        public ComputeQueryValidatorTest()
        {
            _context = ValidationTestHelper.CreateCustomerContext();
            _productContext = ValidationTestHelper.CreateDerivedProductsContext();
            _validator = new MyComputeValidator(_productContext.DefaultQuerySettings);
        }

        public static TheoryDataSet<AllowedArithmeticOperators, string, string> ArithmeticOperators
        {
            get
            {
                return new TheoryDataSet<AllowedArithmeticOperators, string, string>
                {
                    { AllowedArithmeticOperators.Add, "UnitPrice add 0 eq 23", "Add" },
                    { AllowedArithmeticOperators.Divide, "UnitPrice div 23 eq 1", "Divide" },
                    { AllowedArithmeticOperators.Modulo, "UnitPrice mod 23 eq 0", "Modulo" },
                    { AllowedArithmeticOperators.Multiply, "UnitPrice mul 1 eq 23", "Multiply" },
                    { AllowedArithmeticOperators.Subtract, "UnitPrice sub 0 eq 23", "Subtract" },
                };
            }
        }

        public static TheoryDataSet<string> LongInputs
        {
            get
            {
                return GetLongInputsTestData(100);
            }
        }

        public static TheoryDataSet<string> CloseToLongInputs
        {
            get
            {
                return GetLongInputsTestData(95);
            }
        }

        [Fact]
        public void ValidateThrowsOnNullOption()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() =>
                _validator.Validate(null, new ODataValidationSettings()));
        }

        [Fact]
        public void ValidateThrowsOnNullSettings()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() =>
                _validator.Validate(new ComputeQueryOption("Name eq 'abc'", _context), null));
        }

        [Theory]
        [MemberData(nameof(LongInputs))]
        public void LongInputs_CauseMaxNodeCountExceededException(string filter)
        {
            // Arrange
            ODataValidationSettings settings = new ODataValidationSettings
            {
                MaxAnyAllExpressionDepth = Int32.MaxValue
            };

            ComputeQueryOption option = new ComputeQueryOption(filter, _productContext);

            // Act & Assert
            ExceptionAssert.Throws<ODataException>(() => _validator.Validate(option, settings), "The node count limit of '100' has been exceeded. To increase the limit, set the 'MaxNodeCount' property on EnableQueryAttribute or ODataValidationSettings.");
        }

        [Theory]
        [MemberData(nameof(LongInputs))]
        public void IncreaseMaxNodeCountWillAllowLongInputs(string filter)
        {
            // Arrange
            ODataValidationSettings settings = new ODataValidationSettings
            {
                MaxAnyAllExpressionDepth = Int32.MaxValue,
                MaxNodeCount = 105,
            };

            ComputeQueryOption option = new ComputeQueryOption(filter, _productContext);

            // Act & Assert
            ExceptionAssert.DoesNotThrow(() => _validator.Validate(option, settings));
        }

        [Fact]
        public void ArithmeticOperatorsDataSet_CoversAllValues()
        {
            // Arrange
            // Get all values in the AllowedArithmeticOperators enum.
            var values = new HashSet<AllowedArithmeticOperators>(
                Enum.GetValues(typeof(AllowedArithmeticOperators)).Cast<AllowedArithmeticOperators>());
            var groupValues = new[]
            {
                AllowedArithmeticOperators.All,
                AllowedArithmeticOperators.None,
            };

            // Act
            // Remove the group items.
            foreach (var allowed in groupValues)
            {
                values.Remove(allowed);
            }

            // Remove the individual items.
            foreach (var allowed in ArithmeticOperators.Select(item => (AllowedArithmeticOperators)(item[0])))
            {
                values.Remove(allowed);
            }

            // Assert
            // Should have nothing left.
            Assert.Empty(values);
        }

        [Theory]
        [InlineData("Id eq 1 as NewProp")]
        [InlineData("Id ne 1 as NewProp")]
        [InlineData("Id gt 1 as NewProp")]
        [InlineData("Id lt 1 as NewProp")]
        [InlineData("Id ge 1 as NewProp")]
        [InlineData("Id le 1 as NewProp")]
        [InlineData("Id add 1 as NewProp")]
        [InlineData("Id sub 1 as NewProp")]
        [InlineData("Id mul 1 as NewProp")]
        [InlineData("Id div 1 as NewProp")]
        [InlineData("Id mod 1 as NewProp")]
        [InlineData("startswith(Name, 'Microsoft') as NewProp")]
        [InlineData("endswith(Name, 'Microsoft') as NewProp")]
        [InlineData("contains(Name, 'Microsoft') as NewProp")]
        [InlineData("substring(Name, 1) as NewProp")]
        [InlineData("substring(Name, 1, 2) as NewProp")]
        [InlineData("length(Name) as NewProp")]
        [InlineData("tolower(Name) as NewProp")]
        [InlineData("toupper(Name) as NewProp")]
        [InlineData("trim(Name) as NewProp")]
        [InlineData("indexof(Name, 'Microsoft') as NewProp")]
        [InlineData("concat(Name, 'Microsoft') as NewProp")]
        [InlineData("year(Birthday) as NewProp")]
        [InlineData("month(Birthday) as NewProp")]
        [InlineData("day(Birthday) as NewProp")]
        [InlineData("hour(Birthday) as NewProp")]
        [InlineData("minute(Birthday) as NewProp")]
        [InlineData("round(AmountSpent) as NewProp")]
        [InlineData("floor(AmountSpent) as NewProp")]
        [InlineData("ceiling(AmountSpent) as NewProp")]
        // TODO: Support any()/all()
        //[InlineData("Tags/any()")]
        //[InlineData("Tags/all(t : t eq '1')")]
        [InlineData("Microsoft.AspNet.OData.Test.Query.QueryCompositionCustomerBase/Id as NewProp")]
        [InlineData("Contacts/Microsoft.AspNet.OData.Test.Query.QueryCompositionCustomerBase/any() as NewProp")]
        [InlineData("FavoriteColor has Microsoft.AspNet.OData.Test.Builder.TestModels.Color'Red' as NewProp")]
        public void Validator_Doesnot_Throw_For_ValidQueries(string filter)
        {
            // Arrange
            ComputeQueryOption option = new ComputeQueryOption(filter, _context);

            // Act & Assert
            ExceptionAssert.DoesNotThrow(() => _validator.Validate(option, _settings));
        }

        private static TheoryDataSet<string> GetLongInputsTestData(int maxCount)
        {
            return new TheoryDataSet<string>
                {
                    "" + String.Join(" and ", Enumerable.Range(1, (maxCount/5) + 1).Select(_ => "SupplierID eq 1")) + " as NewProp",
                    "" + String.Join(" ", Enumerable.Range(1, maxCount).Select(_ => "not")) + " Discontinued as NewProp",
                    "" + String.Join(" add ", Enumerable.Range(1, maxCount/2)) + " eq 5050 as NewProp",
                    "" + String.Join("/", Enumerable.Range(1, maxCount/2).Select(_ => "Category/Product")) + "/ProductID eq 1 as NewProp",
                    "" + String.Join("/", Enumerable.Range(1, maxCount/2).Select(_ => "Category/Product")) + "/UnsignedReorderLevel eq 1 as NewProp",
                    "" + Enumerable.Range(1,maxCount).Aggregate("'abc'", (prev,i) => String.Format("trim({0})", prev)) + " eq '123' as NewProp",
                    // any() and all() isn't supported in compute for now
                    // " Category/Products/any(" + Enumerable.Range(1,maxCount/4).Aggregate("", (prev,i) => String.Format("p{1}: p{1}/Category/Products/any({0})", prev, i)) +")"
                };
        }

        private class MyComputeValidator : ComputeQueryValidator
        {
            private Dictionary<string, int> _times = new Dictionary<string, int>();

            public MyComputeValidator(DefaultQuerySettings defaultQuerySettings)
                : base(defaultQuerySettings)
            {
            }

            public Dictionary<string, int> Times
            {
                get
                {
                    return _times;
                }
            }

            public override void Validate(ComputeQueryOption computeQueryOption, ODataValidationSettings settings)
            {
                IncrementCount("Validate");
                base.Validate(computeQueryOption, settings);
            }

            public override void ValidateAllNode(AllNode allQueryNode, ODataValidationSettings settings)
            {
                IncrementCount("ValidateAllQueryNode");
                base.ValidateAllNode(allQueryNode, settings);
            }

            public override void ValidateAnyNode(AnyNode anyQueryNode, ODataValidationSettings settings)
            {
                IncrementCount("ValidateAnyQueryNode");
                base.ValidateAnyNode(anyQueryNode, settings);
            }

            public override void ValidateArithmeticOperator(BinaryOperatorNode binaryNode, ODataValidationSettings settings)
            {
                IncrementCount("ValidateArithmeticOperator");
                base.ValidateArithmeticOperator(binaryNode, settings);
            }

            public override void ValidateBinaryOperatorNode(BinaryOperatorNode binaryOperatorNode, ODataValidationSettings settings)
            {
                IncrementCount("ValidateBinaryOperatorQueryNode");
                base.ValidateBinaryOperatorNode(binaryOperatorNode, settings);
            }

            public override void ValidateConstantNode(ConstantNode constantNode, ODataValidationSettings settings)
            {
                IncrementCount("ValidateConstantQueryNode");
                base.ValidateConstantNode(constantNode, settings);
            }

            public override void ValidateConvertNode(ConvertNode convertQueryNode, ODataValidationSettings settings)
            {
                IncrementCount("ValidateConvertQueryNode");
                base.ValidateConvertNode(convertQueryNode, settings);
            }

            public override void ValidateLogicalOperator(BinaryOperatorNode binaryNode, ODataValidationSettings settings)
            {
                IncrementCount("ValidateLogicalOperator");
                base.ValidateLogicalOperator(binaryNode, settings);
            }

            public override void ValidateNavigationPropertyNode(QueryNode sourceNode, IEdmNavigationProperty navigationProperty, ODataValidationSettings settings)
            {
                IncrementCount("ValidateNavigationPropertyNode");
                base.ValidateNavigationPropertyNode(sourceNode, navigationProperty, settings);
            }

            public override void ValidateRangeVariable(RangeVariable rangeVariable, ODataValidationSettings settings)
            {
                IncrementCount("ValidateParameterQueryNode");
                base.ValidateRangeVariable(rangeVariable, settings);
            }

            public override void ValidateSingleValuePropertyAccessNode(SingleValuePropertyAccessNode propertyAccessNode, ODataValidationSettings settings)
            {
                IncrementCount("ValidateSingleValuePropertyAccessNode");
                base.ValidateSingleValuePropertyAccessNode(propertyAccessNode, settings);
            }

            public override void ValidateSingleComplexNode(SingleComplexNode singleComplexNode, ODataValidationSettings settings)
            {
                IncrementCount("ValidateSingleComplexNode");
                base.ValidateSingleComplexNode(singleComplexNode, settings);
            }

            public override void ValidateCollectionPropertyAccessNode(CollectionPropertyAccessNode propertyAccessNode, ODataValidationSettings settings)
            {
                IncrementCount("ValidateCollectionPropertyAccessNode");
                base.ValidateCollectionPropertyAccessNode(propertyAccessNode, settings);
            }

            public override void ValidateCollectionComplexNode(CollectionComplexNode collectionComplexNode, ODataValidationSettings settings)
            {
                IncrementCount("ValidateCollectionComplexNode");
                base.ValidateCollectionComplexNode(collectionComplexNode, settings);
            }

            public override void ValidateSingleValueFunctionCallNode(SingleValueFunctionCallNode node, ODataValidationSettings settings)
            {
                IncrementCount("ValidateSingleValueFunctionCallQueryNode");
                base.ValidateSingleValueFunctionCallNode(node, settings);
            }

            public override void ValidateUnaryOperatorNode(UnaryOperatorNode unaryOperatorQueryNode, ODataValidationSettings settings)
            {
                IncrementCount("ValidateUnaryOperatorQueryNode");
                base.ValidateUnaryOperatorNode(unaryOperatorQueryNode, settings);
            }

            private void IncrementCount(string functionName)
            {
                int count = 0;
                if (_times.TryGetValue(functionName, out count))
                {
                    _times[functionName] = ++count;
                }
                else
                {
                    // first time
                    _times[functionName] = 1;
                }
            }
        }
    }
}
