// namespace Perf.Holders.Generator.Analyzers;
//
// using System.Collections.Immutable;
// using System.Composition;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CodeFixes;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
//
// // [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ResultHolderCodeFix)), Shared]
// sealed class ResultHolderCodeFix : CodeFixProvider {
//     public override ImmutableArray<string> FixableDiagnosticIds => [
//         InvalidHolderDeclarationAnalyzer.OnlyStructRule.Id
//     ];
//
//     public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
//
//     public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
//         var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
//         if (root is null) {
//             return;
//         }
//
//         var diag = context.Diagnostics[0];
//         var diagSpan = diag.Location.SourceSpan;
//
//         var declaration = root.FindToken(diagSpan.Start).Parent?.AncestorsAndSelf()
//             .OfType<StructDeclarationSyntax>()
//             .First();
//         _ = declaration;
//     }
// }


