﻿namespace WpfAnalyzers.Test.WPF0051XmlnsDefinitionMustMapExistingNamespaceTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly WPF0051XmlnsDefinitionMustMapExistingNamespace Analyzer = new WPF0051XmlnsDefinitionMustMapExistingNamespace();

        [Test]
        public void WhenXmlnsDefinitionMatches()
        {
            var controlCode = @"namespace Gu.Wpf.Geometry
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(
            nameof(Brush),
            typeof(Brush),
            typeof(FooControl),
            new PropertyMetadata(default(Brush)));

        public Brush Brush
        {
            get { return (Brush)this.GetValue(BrushProperty); }
            set { this.SetValue(BrushProperty, value); }
        }
    }
}";
            var testCode = @"
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;

[assembly: AssemblyTitle(""Gu.Wpf.Geometry"")]
[assembly: AssemblyDescription(""Geometries for WPF."")]
[assembly: AssemblyConfiguration("""")]
[assembly: AssemblyCompany(""Johan Larsson"")]
[assembly: AssemblyProduct(""Gu.Wpf.Geometry"")]
[assembly: AssemblyCopyright(""Copyright ©  2015"")]
[assembly: AssemblyTrademark("""")]
[assembly: AssemblyCulture("""")]
[assembly: ComVisible(false)]
[assembly: Guid(""a9cbadf5-65f8-4a81-858c-ea0cdfeb2e99"")]
[assembly: AssemblyVersion(""1.0.0.2"")]
[assembly: AssemblyFileVersion(""1.0.0.2"")]
[assembly: InternalsVisibleTo(""Gu.Wpf.Geometry.Tests"", AllInternalsVisible = true)]
[assembly: InternalsVisibleTo(""Gu.Wpf.Geometry.Demo"", AllInternalsVisible = true)]
[assembly: InternalsVisibleTo(""Gu.Wpf.Geometry.Benchmarks"", AllInternalsVisible = true)]

[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
[assembly: XmlnsDefinition(""http://gu.se/Geometry"", ""Gu.Wpf.Geometry"")]";
            AnalyzerAssert.Valid(Analyzer, controlCode, testCode);
        }

        [Test]
        public void WhenTwoXmlnsDefinitions()
        {
            var testCode = @"
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;

[assembly: AssemblyTitle(""Gu.Wpf.Geometry"")]
[assembly: AssemblyDescription(""Geometries for WPF."")]
[assembly: AssemblyConfiguration("""")]
[assembly: AssemblyCompany(""Johan Larsson"")]
[assembly: AssemblyProduct(""Gu.Wpf.Geometry"")]
[assembly: AssemblyCopyright(""Copyright ©  2015"")]
[assembly: AssemblyTrademark("""")]
[assembly: AssemblyCulture("""")]
[assembly: ComVisible(false)]
[assembly: Guid(""a9cbadf5-65f8-4a81-858c-ea0cdfeb2e99"")]
[assembly: AssemblyVersion(""1.0.0.2"")]
[assembly: AssemblyFileVersion(""1.0.0.2"")]
[assembly: InternalsVisibleTo(""Gu.Wpf.Geometry.Tests"", AllInternalsVisible = true)]
[assembly: InternalsVisibleTo(""Gu.Wpf.Geometry.Demo"", AllInternalsVisible = true)]
[assembly: InternalsVisibleTo(""Gu.Wpf.Geometry.Benchmarks"", AllInternalsVisible = true)]

[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
[assembly: XmlnsDefinition(""http://gu.se/Geometry"", ""Gu.Wpf.Geometry"")]
[assembly: XmlnsDefinition(""http://gu.se/Geometry"", ""Gu.Wpf.Geometry.Balloons"")]

namespace Gu.Wpf.Geometry
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public class FooControl1 : Control
    {
        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(
            nameof(Brush),
            typeof(Brush),
            typeof(FooControl1),
            new PropertyMetadata(default(Brush)));

        public Brush Brush
        {
            get { return (Brush)this.GetValue(BrushProperty); }
            set { this.SetValue(BrushProperty, value); }
        }
    }
}

namespace Gu.Wpf.Geometry.Balloons
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public class FooControl2 : Control
    {
        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(
            nameof(Brush),
            typeof(Brush),
            typeof(FooControl2),
            new PropertyMetadata(default(Brush)));

        public Brush Brush
        {
            get { return (Brush)this.GetValue(BrushProperty); }
            set { this.SetValue(BrushProperty, value); }
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
