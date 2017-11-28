﻿namespace WpfAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class WPF0031FieldOrder : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "WPF0031";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "DependencyPropertyKey field must come before DependencyProperty field.",
            messageFormat: "Field '{0}' must come before '{1}'",
            category: AnalyzerCategory.DependencyProperties,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "DependencyPropertyKey field must come before DependencyProperty field.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleDeclaration, SyntaxKind.FieldDeclaration);
        }

        private static void HandleDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is FieldDeclarationSyntax fieldDeclaration &&
                BackingFieldOrProperty.TryCreate(context.ContainingSymbol, out var field) &&
                DependencyProperty.IsPotentialDependencyPropertyBackingField(field) &&
                DependencyProperty.TryGetDependencyPropertyKeyField(field, context.SemanticModel, context.CancellationToken, out var keyField))
            {
                if (field.ContainingType != keyField.ContainingType)
                {
                    return;
                }

                if (keyField.TryGetSyntaxReference(out var reference))
                {
                    var keyNode = reference.GetSyntax(context.CancellationToken);
                    if (!ReferenceEquals(fieldDeclaration.SyntaxTree, keyNode.SyntaxTree) ||
                        fieldDeclaration.SpanStart < keyNode.SpanStart)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(Descriptor, fieldDeclaration.GetLocation(), keyField.Name, field.Name));
                    }
                }
            }
        }
    }
}