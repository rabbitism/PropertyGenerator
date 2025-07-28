using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PropertyGenerator.Avalonia.Generator;
using System.Collections.Immutable;
using System.Reflection;
using Xunit;

namespace PropertyGenerator.Avalonia.Generator.Tests;

public class StyledPropertyGeneratorTests
{
    private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
    private static readonly MetadataReference SystemReference = MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).Assembly.Location);
    private static readonly MetadataReference RuntimeReference = MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location);

    [Fact]
    public void Generator_WithSimpleProperty_GeneratesExpectedCode()
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
        [GeneratedStyledProperty(""default value"")]
        public partial string TestProperty { get; set; }
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        Assert.NotEmpty(result.Results);
        var generatorResult = result.Results[0];
        Assert.NotEmpty(generatorResult.GeneratedSources);
        
        var generatedSource = generatorResult.GeneratedSources[0];
        Assert.Contains("TestPropertyProperty", generatedSource.SourceText.ToString());
        Assert.Contains("public partial string TestProperty", generatedSource.SourceText.ToString());
        Assert.Contains("GetValue", generatedSource.SourceText.ToString());
        Assert.Contains("SetValue", generatedSource.SourceText.ToString());
    }

    [Fact]
    public void Generator_WithIntegerProperty_GeneratesExpectedCode()
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
        [GeneratedStyledProperty(42)]
        public partial int Number { get; set; }
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
        Assert.Contains("NumberProperty", sourceText);
        Assert.Contains("public partial int Number", sourceText);
        Assert.Contains("42", sourceText);
    }

    [Fact]
    public void Generator_WithPropertyCallbacks_GeneratesCallbackMethods()
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
        [GeneratedStyledProperty(""test"")]
        public partial string TestProperty { get; set; }
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
        
        // Should generate partial callback methods
        Assert.Contains("OnTestPropertyPropertyChanged", sourceText);
        Assert.Contains("partial void", sourceText);
    }

    [Fact]
    public void Generator_WithDoNotGeneratePropertyChangedAttribute_SkipsCallbacks()
    {
        // Arrange
        string source = @"
using System;
using Avalonia;
using PropertyGenerator.Avalonia;

namespace TestNamespace
{
    [DoNotGenerateOnPropertyChanged]
    public partial class TestClass : Avalonia.StyledElement
    {
        [GeneratedStyledProperty(""test"")]
        public partial string TestProperty { get; set; }
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
        
        // Should not generate OnPropertyChanged override when DoNotGenerateOnPropertyChanged is applied
        Assert.DoesNotContain("OnPropertyChanged", sourceText);
    }

    [Fact]
    public void Generator_WithMultipleProperties_GeneratesAllProperties()
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
        [GeneratedStyledProperty(""first"")]
        public partial string FirstProperty { get; set; }

        [GeneratedStyledProperty(123)]
        public partial int SecondProperty { get; set; }
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
        
        Assert.Contains("FirstPropertyProperty", sourceText);
        Assert.Contains("SecondPropertyProperty", sourceText);
        Assert.Contains("public partial string FirstProperty", sourceText);
        Assert.Contains("public partial int SecondProperty", sourceText);
    }

    [Fact]
    public void Generator_WithNonStyledElementClass_DoesNotGenerate()
    {
        // Arrange
        string source = @"
using System;
using PropertyGenerator.Avalonia;

namespace TestNamespace
{
    public partial class TestClass
    {
        [GeneratedStyledProperty(""test"")]
        public partial string TestProperty { get; set; }
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert - Should not generate anything because class doesn't inherit from StyledElement
        Assert.Empty(result.Results[0].GeneratedSources);
    }

    [Fact]
    public void Generator_WithoutPartialModifier_DoesNotProcess()
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
        [GeneratedStyledProperty(""test"")]
        public string TestProperty { get; set; }
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert - Should not generate anything because property is not partial
        Assert.Empty(result.Results[0].GeneratedSources);
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
        public static StyledProperty<TValue> Register<TOwner, TValue>(string name, TValue defaultValue = default(TValue)) => new StyledProperty<TValue>();
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