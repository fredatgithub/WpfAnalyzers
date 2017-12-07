﻿namespace WpfAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RenameMemberCodeFixProvider))]
    [Shared]
    internal class RenameMemberCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(WPF0102EventDeclarationName.DiagnosticId);

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var syntaxRoot = await document.GetSyntaxRootAsync(context.CancellationToken)
                                           .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText))
                {
                    continue;
                }

                var memberDeclaration = syntaxRoot.FindNode(diagnostic.Location.SourceSpan)
                                                    .FirstAncestorOrSelf<MemberDeclarationSyntax>();
                if (memberDeclaration == null ||
                    memberDeclaration.IsMissing)
                {
                    continue;
                }

                if (diagnostic.Id == WPF0102EventDeclarationName.DiagnosticId &&
                    diagnostic.Properties.TryGetValue("RegisteredName", out var registeredName))
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            $"Rename to: {registeredName}.",
                            _ => ApplyFixAsync(context, token, registeredName),
                            this.GetType().FullName),
                        diagnostic);
                }
            }
        }

        private static async Task<Solution> ApplyFixAsync(CodeFixContext context, SyntaxToken token, string newName)
        {
            var document = context.Document;
            var syntaxRoot = await document.GetSyntaxRootAsync(context.CancellationToken)
                                           .ConfigureAwait(false);
            return await RenameHelper.RenameSymbolAsync(document, syntaxRoot, token, newName, context.CancellationToken)
                                     .ConfigureAwait(false);
        }
    }
}