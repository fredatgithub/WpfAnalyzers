﻿namespace WpfAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MarkupExtensionReturnTypeArgumentFix))]
    [Shared]
    internal class MarkupExtensionReturnTypeArgumentFix : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(WPF0081MarkupExtensionReturnTypeMustUseCorrectType.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var syntaxRoot = await document.GetSyntaxRootAsync(context.CancellationToken)
                                           .ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken)
                                              .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText))
                {
                    continue;
                }

                var argument = syntaxRoot.FindNode(diagnostic.Location.SourceSpan)
                                         .FirstAncestorOrSelf<AttributeArgumentSyntax>();
                var attribute = argument.FirstAncestor<AttributeSyntax>();
                if (MarkupExtension.TryGetReturnType(attribute.FirstAncestor<ClassDeclarationSyntax>(), semanticModel, context.CancellationToken, out var returnType))
                {
                    context.RegisterDocumentEditorFix(
                        $"Change type to {returnType}.",
                        (e, _) => FixType(e, argument, returnType),
                        this.GetType(),
                        diagnostic);
                }
            }
        }

        private static void FixType(DocumentEditor editor, AttributeArgumentSyntax argument, ITypeSymbol returnType)
        {
            editor.ReplaceNode(
                argument.Expression,
                editor.Generator.TypeOfExpression(editor.Generator.TypeExpression(returnType)));
        }
    }
}