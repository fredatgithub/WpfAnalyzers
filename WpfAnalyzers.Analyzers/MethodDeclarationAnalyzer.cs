﻿namespace WpfAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class MethodDeclarationAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            WPF0004ClrMethodShouldMatchRegisteredName.Descriptor,
            WPF0013ClrMethodMustMatchRegisteredType.Descriptor,
            WPF0033UseAttachedPropertyBrowsableForTypeAttribute.Descriptor,
            WPF0034AttachedPropertyBrowsableForTypeAttributeArgument.Descriptor,
            WPF0042AvoidSideEffectsInClrAccessors.Descriptor,
            WPF0061ClrMethodShouldHaveDocs.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.MethodDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is MethodDeclarationSyntax methodDeclaration &&
                context.ContainingSymbol is IMethodSymbol method &&
                method.IsStatic &&
                method.Parameters.TryGetAtIndex(0, out var parameter) &&
                parameter.Type.Is(KnownSymbol.DependencyObject))
            {
                if (ClrMethod.IsAttachedSetMethod(methodDeclaration, context.SemanticModel, context.CancellationToken, out var call, out var fieldOrProperty))
                {
                    if (DependencyProperty.TryGetRegisteredName(fieldOrProperty, context.SemanticModel, context.CancellationToken, out var registeredName) &&
                        !method.Name.IsParts("Set", registeredName))
                    {
                        context.ReportDiagnostic(
                        Diagnostic.Create(
                            WPF0004ClrMethodShouldMatchRegisteredName.Descriptor,
                            methodDeclaration.Identifier.GetLocation(),
                            method.Name,
                            "Set" + registeredName));
                    }

                    if (DependencyProperty.TryGetRegisteredType(fieldOrProperty, context.SemanticModel, context.CancellationToken, out var registeredType) &&
                        !method.Parameters[1].Type.IsSameType(registeredType))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                WPF0013ClrMethodMustMatchRegisteredType.Descriptor,
                                methodDeclaration.ParameterList.Parameters[1].GetLocation(),
                                "Value type",
                                registeredType));
                    }
                }
                else if (ClrMethod.IsAttachedGetMethod(methodDeclaration, context.SemanticModel, context.CancellationToken, out call, out fieldOrProperty))
                {
                    if (DependencyProperty.TryGetRegisteredName(fieldOrProperty, context.SemanticModel, context.CancellationToken, out var registeredName) &&
                        !method.Name.IsParts("Get", registeredName))
                    {
                        context.ReportDiagnostic(
                        Diagnostic.Create(
                            WPF0004ClrMethodShouldMatchRegisteredName.Descriptor,
                            methodDeclaration.Identifier.GetLocation(),
                            method.Name,
                            "Get" + registeredName));
                    }

                    if (DependencyProperty.TryGetRegisteredType(fieldOrProperty, context.SemanticModel, context.CancellationToken, out var registeredType))
                    {
                        if (!method.ReturnType.IsSameType(registeredType))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    WPF0013ClrMethodMustMatchRegisteredType.Descriptor,
                                    methodDeclaration.ReturnType.GetLocation(),
                                    "Return type",
                                    registeredType));
                        }
                    }

                    if (AttachedPropertyBrowsableForType.TryGetAttribute(methodDeclaration, context.SemanticModel, context.CancellationToken, out var attribute))
                    {
                        if (attribute.ArgumentList != null &&
                            attribute.ArgumentList.Arguments.TryGetSingle(out var attributeArgument) &&
                            TypeOf.TryGetType(attributeArgument.Expression as TypeOfExpressionSyntax, context.SemanticModel, context.CancellationToken, out var argumentType) &&
                            !ReferenceEquals(parameter.Type, argumentType))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    WPF0034AttachedPropertyBrowsableForTypeAttributeArgument.Descriptor,
                                    attributeArgument.GetLocation(),
                                    parameter.Type.ToMinimalDisplayString(
                                        context.SemanticModel,
                                        attributeArgument.SpanStart)));
                        }
                    }
                    else
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                    WPF0033UseAttachedPropertyBrowsableForTypeAttribute.Descriptor,
                                    methodDeclaration.Identifier.GetLocation(),
                                    parameter.Type.ToMinimalDisplayString(context.SemanticModel, methodDeclaration.SpanStart)));
                    }
                }
                else
                {
                    return;
                }

                if (!methodDeclaration.HasDocumentation() &&
                    (method.DeclaredAccessibility == Accessibility.Public ||
                     method.DeclaredAccessibility == Accessibility.Internal))
                {
                    context.ReportDiagnostic(Diagnostic.Create(WPF0061ClrMethodShouldHaveDocs.Descriptor, methodDeclaration.GetLocation()));
                }

                if (call != null &&
                    methodDeclaration.Body is BlockSyntax body &&
                    body.Statements.TryGetFirst(x => !x.Contains(call), out var statement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(WPF0042AvoidSideEffectsInClrAccessors.Descriptor, statement.GetLocation()));
                }
            }
        }
    }
}