namespace WpfAnalyzers.Test.WPF0002BackingFieldShouldMatchRegisteredNameTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        [TestCase("\"Bar\"")]
        [TestCase("nameof(Bar)")]
        [TestCase("nameof(FooControl.Bar)")]
        public void ReadOnlyDependencyProperty(string nameof)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        private static readonly DependencyPropertyKey BarPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Bar),
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public static readonly DependencyProperty BarProperty = BarPropertyKey.DependencyProperty;
    
        public int Bar
        {
            get { return (int)GetValue(BarProperty); }
            set { SetValue(BarProperty, value); }
        }
    }
}";
            testCode = testCode.AssertReplace("nameof(Bar)", nameof);
            AnalyzerAssert.Valid<WPF0002BackingFieldShouldMatchRegisteredName>(testCode);
        }

        [Test]
        public void ReadOnlyDependencyPropertyRepro()
        {
            var statusCode = @"
namespace RoslynSandbox
{
    public enum Status
    {
        Idle,
        Updating
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        internal static readonly DependencyPropertyKey StatusPropertyKey = DependencyProperty.RegisterReadOnly(
            ""Status"",
            typeof(Status),
            typeof(FooControl),
            new PropertyMetadata(Status.Idle, OnStatusChanged));

        internal static readonly DependencyProperty StatusProperty = StatusPropertyKey.DependencyProperty;

        internal Status Status
        {
            get { return (Status)this.GetValue(StatusProperty); }
            set { this.SetValue(StatusProperty, value); }
        }

        private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // nop
        }
    }
}";
            AnalyzerAssert.Valid<WPF0002BackingFieldShouldMatchRegisteredName>(statusCode, testCode);
        }

        [Test]
        public void ReadOnlyAttachedProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        private static readonly DependencyPropertyKey BarPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            ""Bar"",
            typeof(int),
            typeof(Foo),
            new PropertyMetadata(default(int)));

        public static readonly DependencyProperty BarProperty = BarPropertyKey.DependencyProperty;

        public static void SetBar(DependencyObject element, int value)
        {
            element.SetValue(BarPropertyKey, value);
        }

        public static int GetBar(DependencyObject element)
        {
            return (int)element.GetValue(BarProperty);
        }
    }
}";

            AnalyzerAssert.Valid<WPF0002BackingFieldShouldMatchRegisteredName>(testCode);
        }

        [Test]
        public void IgnoresDependencyProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ErrorProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get { return (int)GetValue(ErrorProperty); }
            set { SetValue(ErrorProperty, value); }
        }
    }
}";
            AnalyzerAssert.Valid<WPF0002BackingFieldShouldMatchRegisteredName>(testCode);
        }

        [Test]
        public void IgnoresAttachedProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        public static readonly DependencyProperty ErrorProperty = DependencyProperty.RegisterAttached(
            ""Bar"",
            typeof(int),
            typeof(Foo),
            new PropertyMetadata(default(int)));

        public static void SetBar(DependencyObject element, int value)
        {
            element.SetValue(ErrorProperty, value);
        }

        public static int GetBar(DependencyObject element)
        {
            return (int)element.GetValue(ErrorProperty);
        }
    }
}";

            AnalyzerAssert.Valid<WPF0002BackingFieldShouldMatchRegisteredName>(testCode);
        }
    }
}