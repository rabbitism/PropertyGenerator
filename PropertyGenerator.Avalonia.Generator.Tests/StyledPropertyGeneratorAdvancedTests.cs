using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PropertyGenerator.Avalonia.Generator;
using System.Collections.Immutable;
using System.Reflection;
using Xunit;

namespace PropertyGenerator.Avalonia.Generator.Tests;

public class StyledPropertyGeneratorAdvancedTests
{
    private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
    private static readonly MetadataReference SystemReference = MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).Assembly.Location);
    private static readonly MetadataReference RuntimeReference = MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location);

    [Fact]
    public void Generator_WithDefaultValueCallback_GeneratesCallbackReference()
    {
        // Arrange
        string source = @"
using System;
using Avalonia;
using PropertyGenerator.Avalonia;

namespace TestNamespace
{
    public partial class TestClass : Avalonia.StyledElement
    {
        [GeneratedStyledProperty(DefaultValueCallback = nameof(GetDefaultValue))]
        public partial string TestProperty { get; set; }

        private static string GetDefaultValue() => ""default"";
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        Assert.NotEmpty(result.Results);
        var generatorResult = result.Results[0];
        Assert.NotEmpty(generatorResult.GeneratedSources);
        
        var generatedSource = generatorResult.GeneratedSources[0];
        var sourceText = generatedSource.SourceText.ToString();
        
        // Should reference the callback method
        Assert.Contains("GetDefaultValue", sourceText);
    }

    [Fact]
    public void Generator_WithValidationParameter_GeneratesValidationReference()
    {
        // Arrange
        string source = @"
using System;
using Avalonia;
using PropertyGenerator.Avalonia;

namespace TestNamespace
{
    public partial class TestClass : Avalonia.StyledElement
    {
        [GeneratedStyledProperty(""test"", Validate = nameof(ValidateValue))]
        public partial string TestProperty { get; set; }

        private static bool ValidateValue(string value) => !string.IsNullOrEmpty(value);
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        Assert.NotEmpty(result.Results);
        var generatorResult = result.Results[0];
        Assert.NotEmpty(generatorResult.GeneratedSources);
        
        var generatedSource = generatorResult.GeneratedSources[0];
        var sourceText = generatedSource.SourceText.ToString();
        
        // Should reference the validation method
        Assert.Contains("ValidateValue", sourceText);
    }

    [Fact]
    public void Generator_WithCoerceParameter_GeneratesCoerceReference()
    {
        // Arrange
        string source = @"
using System;
using Avalonia;
using PropertyGenerator.Avalonia;

namespace TestNamespace
{
    public partial class TestClass : Avalonia.StyledElement
    {
        [GeneratedStyledProperty(""test"", Coerce = nameof(CoerceValue))]
        public partial string TestProperty { get; set; }

        private static string CoerceValue(AvaloniaObject obj, string value) => value?.Trim();
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        Assert.NotEmpty(result.Results);
        var generatorResult = result.Results[0];
        Assert.NotEmpty(generatorResult.GeneratedSources);
        
        var generatedSource = generatorResult.GeneratedSources[0];
        var sourceText = generatedSource.SourceText.ToString();
        
        // Should reference the coerce method
        Assert.Contains("CoerceValue", sourceText);
    }

    [Fact]
    public void Generator_WithBooleanProperty_GeneratesCorrectType()
    {
        // Arrange
        string source = @"
using System;
using Avalonia;
using PropertyGenerator.Avalonia;

namespace TestNamespace
{
    public partial class TestClass : Avalonia.StyledElement
    {
        [GeneratedStyledProperty(true)]
        public partial bool IsEnabled { get; set; }
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        Assert.NotEmpty(result.Results);
        var generatorResult = result.Results[0];
        Assert.NotEmpty(generatorResult.GeneratedSources);
        
        var generatedSource = generatorResult.GeneratedSources[0];
        var sourceText = generatedSource.SourceText.ToString();
        
        Assert.Contains("bool IsEnabled", sourceText);
        Assert.Contains("IsEnabledProperty", sourceText);
        Assert.Contains("true", sourceText);
    }

    [Fact]
    public void Generator_WithNullableProperty_GeneratesCorrectType()
    {
        // Arrange
        string source = @"
using System;
using Avalonia;
using PropertyGenerator.Avalonia;

namespace TestNamespace
{
    public partial class TestClass : Avalonia.StyledElement
    {
        [GeneratedStyledProperty(null)]
        public partial string? OptionalText { get; set; }
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        Assert.NotEmpty(result.Results);
        var generatorResult = result.Results[0];
        Assert.NotEmpty(generatorResult.GeneratedSources);
        
        var generatedSource = generatorResult.GeneratedSources[0];
        var sourceText = generatedSource.SourceText.ToString();
        
        Assert.Contains("string? OptionalText", sourceText);
        Assert.Contains("OptionalTextProperty", sourceText);
    }

    [Fact]
    public void Generator_WithComplexPropertyConfiguration_GeneratesAllFeatures()
    {
        // Arrange
        string source = @"
using System;
using Avalonia;
using PropertyGenerator.Avalonia;

namespace TestNamespace
{
    public partial class TestClass : Avalonia.StyledElement
    {
        [GeneratedStyledProperty(
            ""default"",
            Validate = nameof(ValidateText),
            Coerce = nameof(CoerceText),
            EnableDataValidation = true,
            Inherits = true)]
        public partial string ComplexProperty { get; set; }

        private static bool ValidateText(string value) => true;
        private static string CoerceText(AvaloniaObject obj, string value) => value;
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        Assert.NotEmpty(result.Results);
        var generatorResult = result.Results[0];
        Assert.NotEmpty(generatorResult.GeneratedSources);
        
        var generatedSource = generatorResult.GeneratedSources[0];
        var sourceText = generatedSource.SourceText.ToString();
        
        Assert.Contains("ComplexPropertyProperty", sourceText);
        Assert.Contains("ValidateText", sourceText);
        Assert.Contains("CoerceText", sourceText);
        Assert.Contains("true", sourceText); // enableDataValidation and inherits
    }

    [Fact]
    public void Generator_WithInternalProperty_GeneratesInternalAccessibility()
    {
        // Arrange
        string source = @"
using System;
using Avalonia;
using PropertyGenerator.Avalonia;

namespace TestNamespace
{
    public partial class TestClass : Avalonia.StyledElement
    {
        [GeneratedStyledProperty(""internal"")]
        internal partial string InternalProperty { get; set; }
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        Assert.NotEmpty(result.Results);
        var generatorResult = result.Results[0];
        Assert.NotEmpty(generatorResult.GeneratedSources);
        
        var generatedSource = generatorResult.GeneratedSources[0];
        var sourceText = generatedSource.SourceText.ToString();
        
        Assert.Contains("internal partial string InternalProperty", sourceText);
        Assert.Contains("internal static", sourceText); // For the property field
    }

    [Fact]
    public void Generator_WithNestedClass_GeneratesCorrectNamespace()
    {
        // Arrange
        string source = @"
using System;
using Avalonia;
using PropertyGenerator.Avalonia;

namespace TestNamespace.Nested
{
    public partial class TestClass : Avalonia.StyledElement
    {
        [GeneratedStyledProperty(""nested"")]
        public partial string NestedProperty { get; set; }
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        Assert.NotEmpty(result.Results);
        var generatorResult = result.Results[0];
        Assert.NotEmpty(generatorResult.GeneratedSources);
        
        var generatedSource = generatorResult.GeneratedSources[0];
        var sourceText = generatedSource.SourceText.ToString();
        
        Assert.Contains("namespace TestNamespace.Nested", sourceText);
        Assert.Contains("NestedPropertyProperty", sourceText);
    }

    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        // Create the compilation
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        
        // Add references for Avalonia and our attribute types
        var references = new List<MetadataReference>
        {
            CorlibReference,
            SystemReference,
            RuntimeReference
        };

        // Add a mock Avalonia reference by creating in-memory assemblies for the types we need
        var avaloniaSource = @"
namespace Avalonia
{
    public class AvaloniaObject {}
    public class StyledElement : AvaloniaObject {}
    public class StyledProperty<T> {}
    public class AvaloniaProperty 
    {
        public static StyledProperty<TValue> Register<TOwner, TValue>(
            string name, 
            TValue defaultValue = default(TValue),
            bool inherits = false,
            bool enableDataValidation = false,
            System.Func<TValue, bool> validate = null,
            System.Func<AvaloniaObject, TValue, TValue> coerce = null) => new StyledProperty<TValue>();
        public string Name { get; }
    }
    public class AvaloniaPropertyChangedEventArgs 
    {
        public AvaloniaProperty Property { get; }
        public object OldValue { get; }
        public object NewValue { get; }
    }
}
";

        var attributeSource = @"
using System;
namespace PropertyGenerator.Avalonia
{
    [AttributeUsage(AttributeTargets.Property)]
    public class GeneratedStyledPropertyAttribute : Attribute
    {
        public object DefaultValue { get; set; }
        public string DefaultValueCallback { get; set; }
        public string Validate { get; set; }
        public string Coerce { get; set; }
        public bool EnableDataValidation { get; set; }
        public bool Inherits { get; set; }
        public object DefaultBindingMode { get; set; }

        public GeneratedStyledPropertyAttribute() { }
        public GeneratedStyledPropertyAttribute(object defaultValue) { DefaultValue = defaultValue; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DoNotGenerateOnPropertyChangedAttribute : Attribute { }
}
";

        var avaloniaTree = CSharpSyntaxTree.ParseText(avaloniaSource);
        var attributeTree = CSharpSyntaxTree.ParseText(attributeSource);
        
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree, avaloniaTree, attributeTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        // Create the generator
        var generator = new StyledPropertyGenerator();

        // Run the generator
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);

        return driver.GetRunResult();
    }
}